using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECR;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ElasticLoadBalancingV2;
using Amazon.CDK.AWS.IAM;
using System.Collections.Generic;

namespace Infrastructure
{
    public class InfrastructureStack : Stack
    {
        #region Declerations
        // Networking
        Vpc _vpc;

        // ELB
        ApplicationLoadBalancer _alb;
        ApplicationListener _albListener;

        // ECS Fargate
        Cluster _ecsFargateCluster;
        Role _ecsTaskRole;
        Role _ecsTaskExecutionRole;
        SecurityGroup _ecsFargateIngressSecurityGroup;
        FargateTaskDefinition _modernizedService_A__TaskDefinition;
        FargateService _modernizedService_A__ecsServiceDefinition;

        FargateTaskDefinition _modernizedService_B__TaskDefinition;
        FargateService _modernizedService_B__ecsServiceDefinition;
        #endregion

        internal InfrastructureStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            // VPC, Subnets, NATs, IGW, etc. supporting the Legacy and the Modernized resources.
            CreateNetworkLandingZone();

            // ALB that will front the ECS Fargate containers.
            CreateApplicationLoadBalancer();

            OrchestrateEcsFargateResources();
        }

        #region Networking Resources
        /// <summary>
        /// Creates network resources like VPC, Subnets, Route Tables, NAT, Internet Gateways.
        /// </summary>
        private void CreateNetworkLandingZone()
        {
            /* FYI: Propotional to the MaxAzs value (e.g. 2 here), the following AWS resources get yielded: 
            * 1) Total four subnets; pubilc and private subnet pairs in each AZ
            * 2) Two NAT Gateways for high availability
            * 3) One Internet Gateway (IGW)
            * 4) Five Route Tables (RT) with traffic routes configured as following
            *  a) One as the Main RT 
            *  b) Two RT directing traffic from within the Public subnets to the IGW
            *  c) Two RT directing traffic from within the Private subnets to the NAT                 
            */
            _vpc = new Vpc(this, InfrastructureConfigs.vpc_id, new VpcProps
            {
                MaxAzs = 2,
                Cidr = InfrastructureConfigs.vpc_cidr,
                SubnetConfiguration = new[] 
                {
                    new SubnetConfiguration()
                    {
                        Name = InfrastructureConfigs.vpc_public_subnet_name,
                        CidrMask = 24,
                        SubnetType = SubnetType.PUBLIC
                    },
                    new SubnetConfiguration()
                    {
                        Name = InfrastructureConfigs.vpc_private_subnet_name,
                        CidrMask = 24,
                        SubnetType = SubnetType.PRIVATE
                    } 
                }
            });
        }
        #endregion

        #region ELB Resources
        /// <summary>
        /// Create and configure the Application Load Balancer (ALB) and its listener.  This will front the ECS Fargate services.
        /// note: The targets will be added when the ECS Fargate service(s) are created.
        /// </summary>
        private void CreateApplicationLoadBalancer()
        {
            // Controls the internet traffic coming into the ALB
            var alb_ingress_securitygroup_name = new SecurityGroup(this, InfrastructureConfigs.alb_ingress_securitygroup_name, new SecurityGroupProps
            {
                Vpc = _vpc,
                AllowAllOutbound = true
            });
            alb_ingress_securitygroup_name.Connections.AllowToAnyIpv4(Port.Tcp(80));


            // Create and Configure the ALB
            _alb = new ApplicationLoadBalancer(this, InfrastructureConfigs.alb_name, new ApplicationLoadBalancerProps
            {
                LoadBalancerName = InfrastructureConfigs.alb_name,
                Vpc = _vpc,
                InternetFacing = true,
                SecurityGroup = alb_ingress_securitygroup_name
            });

            // Create listener
            _albListener = _alb.AddListener(InfrastructureConfigs.alb_listener_name, new ApplicationListenerProps
            {
                Protocol = ApplicationProtocol.HTTP,
                Port = 80,
                DefaultAction = ListenerAction.FixedResponse(503)
            });
        }
        #endregion

        #region ECS Fargate Resources
        /// <summary>
        /// This orchestrator will spin up following ECS fargate resources
        /// 0. create Application Load Balancer directing internet traffic to the appropriate Workload_* targets; ECS Fargate containers
        /// 1. create ECS Fargate Cluster
        ///  2. create Task Definition
        ///      a. Task roles
        ///      b. Register ECR image
        ///      c. specify Container specs like memory, vCPU, port etc.        
        /// build Service blueprint
        /// Register Task definition
        /// Specify task 'desired state'; number of instances to run
        /// Register VPC
        /// Register Subnet
        /// Disable public IP
        /// Register ELB
        /// Register service discovery
        /// </summary>
        private void OrchestrateEcsFargateResources()
        {
            CreateEcsTaskRole();

            CreateEcsTaskExecutionRole();

            CreateEcsFargateIngressSG();

            CreateEcsFargateCluster();

            // Service_A
            CreateModernizedService_A__EcsFargateTaskDef();
            CreateModernizedService_A__EcsFargateServiceDef();

            // Service_B
            CreateModernizedService_B__EcsFargateTaskDef();
            CreateModernizedService_B__EcsFargateServiceDef();
        }

        /// <summary>
        /// Create IAM role that tasks can use to make API requests to authorized AWS services.
        /// </summary>
        private void CreateEcsTaskRole()
        {
            _ecsTaskRole = new Role(this, InfrastructureConfigs.ecsfargate_task_def_role_name, new RoleProps
            {
                AssumedBy = new ServicePrincipal(InfrastructureConfigs.aws_service_principal_name), // An IAM principal that represents an AWS service (e.g, sqs.amazonaws.com).
                ManagedPolicies = new List<IManagedPolicy>() {
                    ManagedPolicy.FromAwsManagedPolicyName("CloudWatchLogsFullAccess"),
                    ManagedPolicy.FromAwsManagedPolicyName("AWSXRayDaemonWriteAccess")
                }.ToArray()
            });
        }

        /// <summary>
        /// Create role required by tasks to pull container images and publish container logs to Amazon CloudWatch on our behalf.
        /// </summary>
        private void CreateEcsTaskExecutionRole()
        {
            _ecsTaskExecutionRole = new Role(this, InfrastructureConfigs.ecsfargate_task_def_execution_role_name, new RoleProps
            {
                AssumedBy = new ServicePrincipal(InfrastructureConfigs.aws_service_principal_name),
                ManagedPolicies = new List<IManagedPolicy>() {
                     ManagedPolicy.FromAwsManagedPolicyName("AmazonEC2ContainerRegistryReadOnly"),
                    ManagedPolicy.FromAwsManagedPolicyName("CloudWatchLogsFullAccess")
                 }.ToArray()
            });
        }

        /// <summary>
        /// Rules governing the traffic that would come into the app behing hosted within the Task/Container instance.
        /// </summary>
        private void CreateEcsFargateIngressSG()
        {
            _ecsFargateIngressSecurityGroup = new SecurityGroup(this, InfrastructureConfigs.ecsFargate_task_ingress_sg, new SecurityGroupProps
            {
                Vpc = _vpc,
                SecurityGroupName = InfrastructureConfigs.ecsFargate_task_ingress_sg,
                Description = "Firewall for ECS Task ingress.",
                AllowAllOutbound = true
            });

            // Note: Same ports applies to both of the Modernized Services.  So using one of them sufficies the needs.
            _ecsFargateIngressSecurityGroup.AddIngressRule(Peer.AnyIpv4(), Port.Tcp(InfrastructureConfigs.modernizedService_A__app_container_listener_port), "ALB will fwd. the incoming internet traffic on this port");
            _ecsFargateIngressSecurityGroup.AddIngressRule(Peer.AnyIpv4(), Port.Tcp(InfrastructureConfigs.modernizedService_A__app_container_listener_port), "Allow ALB target to perform health checks on the vGW Envoy Proxy");
        }

        /// <summary>
        /// Creates the ECS Farget cluster
        /// </summary>
        private void CreateEcsFargateCluster()
        {
            _ecsFargateCluster = new Cluster(this, InfrastructureConfigs.ecsfargate_cluster_name, new ClusterProps
            {
                ClusterName = InfrastructureConfigs.ecsfargate_cluster_name,
                Vpc = _vpc,
                ContainerInsights = true
            });
        }

        #region Service_A
        /// <summary>
        ///  Creates & configures the ECS Fargate Task definition:
        ///  1. Task roles
        ///  2. Register ECR image
        ///  3. Specify Container specs like memory, vCPU, port etc.
        ///  4. Add ability to read to read senstative information securely; from the AWS secret manager
        /// </summary>
        private void CreateModernizedService_A__EcsFargateTaskDef()
        {
            // Modernized Service_A: create the task definition
            _modernizedService_A__TaskDefinition = new FargateTaskDefinition(this, InfrastructureConfigs.modernizedService_A__ecsfargate_task_def_name, new FargateTaskDefinitionProps
            {
                TaskRole = _ecsTaskRole,
                ExecutionRole = _ecsTaskExecutionRole,
                MemoryLimitMiB = InfrastructureConfigs.modernizedService_A__ecsfargate_task_def_memoryInMB,
                Cpu = InfrastructureConfigs.modernizedService_A__ecsfargate_task_def_cpuUnits
            });

            // Modernized Service_A: register the container definition
            // Note: ensure this AWS ECR repository exists and it has the correct container image in it.
            var workload_A_container = _modernizedService_A__TaskDefinition.AddContainer(InfrastructureConfigs.modernizedService_A__container_name, new ContainerDefinitionProps()
            {
                Image = ContainerImage.FromEcrRepository(Repository.FromRepositoryName(this, InfrastructureConfigs.modernizedService_A__img_repo_name, InfrastructureConfigs.modernizedService_A__img_repo_name))
            });

            // Modernized Service_A: register the container ports
            workload_A_container.AddPortMappings(
                 new PortMapping()
                 {
                     HostPort = InfrastructureConfigs.modernizedService_A__app_container_listener_port,
                     ContainerPort = InfrastructureConfigs.modernizedService_A__app_container_listener_port,
                     Protocol = Amazon.CDK.AWS.ECS.Protocol.TCP
                 }
            );

            // AWS XRay: register the container definition
            var xRayContainerRegistration = _modernizedService_A__TaskDefinition.AddContainer(InfrastructureConfigs.aws_xray__img_repo_name, new ContainerDefinitionProps()
            {
                Image = ContainerImage.FromRegistry(InfrastructureConfigs.aws_xray__img_repo_name),
                Essential = true,
                User = InfrastructureConfigs.container_nonroot_user_id
            });

            // AWS XRay: register the container ports
            xRayContainerRegistration.AddPortMappings(
                new PortMapping
                {
                    Protocol = Amazon.CDK.AWS.ECS.Protocol.UDP,
                    ContainerPort = InfrastructureConfigs.aws_xray__container_port,
                    HostPort = InfrastructureConfigs.aws_xray__container_port
                }
            );
        }

        /// <summary>
        /// Creates & configures the ECS Fargate Service 
        /// </summary>
        private void CreateModernizedService_A__EcsFargateServiceDef()
        {
            // Create and confgirue Fargate service
            _modernizedService_A__ecsServiceDefinition = new FargateService(this, InfrastructureConfigs.modernizedService_A__ecsfargate_service_name, new FargateServiceProps
            {
                ServiceName = InfrastructureConfigs.modernizedService_A__ecsfargate_service_name,
                TaskDefinition = _modernizedService_A__TaskDefinition,
                Cluster = _ecsFargateCluster,
                DesiredCount = InfrastructureConfigs.modernizedService_A__ecsfargate_service_task_desired_count,
                SecurityGroups = new [] { _ecsFargateIngressSecurityGroup },
                AssignPublicIp = false,
                VpcSubnets = new SubnetSelection()
                {
                    OnePerAz = true,
                    SubnetType = SubnetType.PRIVATE
                }
            });

            // Register with the ALB targets
            // Helpful source: https://docs.aws.amazon.com/cdk/api/latest/dotnet/api/Amazon.CDK.AWS.ElasticLoadBalancingV2.html
            _albListener.AddTargets(InfrastructureConfigs.modernizedService_A__alb_listener_targetgroup_name, new AddApplicationTargetsProps
            {
                Priority = 2,
                Conditions = new[] { ListenerCondition.PathPatterns(new[] { InfrastructureConfigs.modernizedService_A__alb_listener_path_pattern }) },
                TargetGroupName = InfrastructureConfigs.modernizedService_A__alb_listener_targetgroup_name,
                Port = InfrastructureConfigs.modernizedService_A__app_container_listener_port,
                Targets = new List<FargateService> {
                    _modernizedService_A__ecsServiceDefinition
                }.ToArray(),
                HealthCheck = new Amazon.CDK.AWS.ElasticLoadBalancingV2.HealthCheck()
                {
                    Path = InfrastructureConfigs.modernizedService_A__alb_listener_targetgroup_healthcheck_path,
                    Port = InfrastructureConfigs.modernizedService_A__app_container_listener_port.ToString(),
                    Protocol = Amazon.CDK.AWS.ElasticLoadBalancingV2.Protocol.HTTP
                }
            });
        }
        #endregion

        #region Service_B
        /// <summary>
        ///  Creates & configures the ECS Fargate Task definition:
        ///  1. Task roles
        ///  2. Register ECR image
        ///  3. Specify Container specs like memory, vCPU, port etc.
        ///  4. Add ability to read to read senstative information securely; from the AWS secret manager
        /// </summary>
        private void CreateModernizedService_B__EcsFargateTaskDef()
        {
            // Modernized Service_A: create the task definition
            _modernizedService_B__TaskDefinition = new FargateTaskDefinition(this, InfrastructureConfigs.modernizedService_B__ecsfargate_task_def_name, new FargateTaskDefinitionProps
            {
                TaskRole = _ecsTaskRole,
                ExecutionRole = _ecsTaskExecutionRole,
                MemoryLimitMiB = InfrastructureConfigs.modernizedService_B__ecsfargate_task_def_memoryInMB,
                Cpu = InfrastructureConfigs.modernizedService_B__ecsfargate_task_def_cpuUnits
            });

            // Modernized Service_A: register the container definition
            // Note: ensure this AWS ECR repository exists and it has the correct container image in it.
            var workload_A_container = _modernizedService_B__TaskDefinition.AddContainer(InfrastructureConfigs.modernizedService_B__container_name, new ContainerDefinitionProps()
            {
                Image = ContainerImage.FromEcrRepository(Repository.FromRepositoryName(this, InfrastructureConfigs.modernizedService_B__img_repo_name, InfrastructureConfigs.modernizedService_B__img_repo_name))
            });

            // Modernized Service_A: register the container ports
            workload_A_container.AddPortMappings(
                 new PortMapping()
                 {
                     HostPort = InfrastructureConfigs.modernizedService_B__app_container_listener_port,
                     ContainerPort = InfrastructureConfigs.modernizedService_B__app_container_listener_port,
                     Protocol = Amazon.CDK.AWS.ECS.Protocol.TCP
                 }
            );

            // AWS XRay: register the container definition
            var xRayContainerRegistration = _modernizedService_B__TaskDefinition.AddContainer(InfrastructureConfigs.aws_xray__img_repo_name, new ContainerDefinitionProps()
            {
                Image = ContainerImage.FromRegistry(InfrastructureConfigs.aws_xray__img_repo_name),
                Essential = true,
                User = InfrastructureConfigs.container_nonroot_user_id
            });

            // AWS XRay: register the container ports
            xRayContainerRegistration.AddPortMappings(
                new PortMapping
                {
                    Protocol = Amazon.CDK.AWS.ECS.Protocol.UDP,
                    ContainerPort = InfrastructureConfigs.aws_xray__container_port,
                    HostPort = InfrastructureConfigs.aws_xray__container_port
                }
            );
        }

        /// <summary>
        /// Creates & configures the ECS Fargate Service 
        /// </summary>
        private void CreateModernizedService_B__EcsFargateServiceDef()
        {
            // Create and confgirue Fargate service
            _modernizedService_B__ecsServiceDefinition = new FargateService(this, InfrastructureConfigs.modernizedService_B__ecsfargate_service_name, new FargateServiceProps
            {
                ServiceName = InfrastructureConfigs.modernizedService_B__ecsfargate_service_name,
                TaskDefinition = _modernizedService_B__TaskDefinition,
                Cluster = _ecsFargateCluster,
                DesiredCount = InfrastructureConfigs.modernizedService_B__ecsfargate_service_task_desired_count,
                SecurityGroups = new[] { _ecsFargateIngressSecurityGroup },
                AssignPublicIp = false,
                VpcSubnets = new SubnetSelection()
                {
                    OnePerAz = true,
                    SubnetType = SubnetType.PRIVATE
                }
            });

            // Register with the ALB targets
            // Helpful source: https://docs.aws.amazon.com/cdk/api/latest/dotnet/api/Amazon.CDK.AWS.ElasticLoadBalancingV2.html
            _albListener.AddTargets(InfrastructureConfigs.modernizedService_B__alb_listener_targetgroup_name, new AddApplicationTargetsProps
            {
                Priority = 3,
                Conditions = new[] { ListenerCondition.PathPatterns(new[] { InfrastructureConfigs.modernizedService_B__alb_listener_path_pattern }) },
                TargetGroupName = InfrastructureConfigs.modernizedService_B__alb_listener_targetgroup_name,
                Port = InfrastructureConfigs.modernizedService_B__app_container_listener_port,
                Targets = new List<FargateService> {
                    _modernizedService_B__ecsServiceDefinition
                }.ToArray(),
                HealthCheck = new Amazon.CDK.AWS.ElasticLoadBalancingV2.HealthCheck()
                {
                    Path = InfrastructureConfigs.modernizedService_B__alb_listener_targetgroup_healthcheck_path,
                    Port = InfrastructureConfigs.modernizedService_B__app_container_listener_port.ToString(),
                    Protocol = Amazon.CDK.AWS.ElasticLoadBalancingV2.Protocol.HTTP
                }
            });
        }
        #endregion

        #endregion
    }
}

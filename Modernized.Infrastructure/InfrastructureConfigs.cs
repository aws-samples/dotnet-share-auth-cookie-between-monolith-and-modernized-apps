namespace Infrastructure
{
    public class InfrastructureConfigs
    {
        #region Globals
        private static string project_name = "shared-cookie";

        // CloudFormation (cft) stack id
        public static string cft_cdk_stackId = $"{project_name}-cdk-stack";

        private static string modernized_service_a_name = "modernized-service-a";
        private static string modernized_service_b_name = "modernized-service-b";

        //private static string workload_A__app_name = "workloadA";

        //private static string workload_B__app_name = "workloadB";
        #endregion

        #region Network Attributes
        // VPC
        public static string vpc_id = $"{project_name}-pvc";
        public static string vpc_cidr = "10.0.0.0/16";
        
        // Subnet   
        public static string vpc_public_subnet_name = $"public-{vpc_id}";
        public static string vpc_private_subnet_name = $"private-{vpc_id}";
        #endregion

        #region Load Balancer Attributes
        // ALB
        public static string alb_ingress_securitygroup_name = $"ingress-modernized-{project_name}";
        public static string alb_name = $"modernized-{project_name}-alb";
        public static string alb_listener_name = $"modernized-services-listener";

        // Service_A
        public static string modernizedService_A__alb_listener_targetgroup_name = $"modernized-serviceA-instances"; // Max of 32 characters allowed
        public static string modernizedService_A__alb_listener_targetgroup_healthcheck_path = "/api/service-a/health";
        public static string modernizedService_A__alb_listener_path_pattern = "/api/service-a*";

        // Service_B
        public static string modernizedService_B__alb_listener_targetgroup_name = $"modernized-serviceB-instances"; // Max of 32 characters allowed
        public static string modernizedService_B__alb_listener_targetgroup_healthcheck_path = "/api/service-b/health";
        public static string modernizedService_B__alb_listener_path_pattern = "/api/service-b*";

        #endregion

        #region ECS Fargate Attributes

        // Task role & Execution role attributes       
        public static string aws_service_principal_name = "ecs-tasks.amazonaws.com"; // This is a built-in IAM principal that will assume the taskDef/taskExec roles as needed.
        public static string ecsfargate_task_def_role_name = $"{project_name}-task-role";
        public static string ecsfargate_task_def_execution_role_name = $"{project_name}-task-execution-role";

        // ECS Fargate Cluster
        public static string ecsfargate_cluster_name = $"{project_name}-ecs-cluster";

        // Security group attributes
        public static string ecsFargate_task_ingress_sg = $"ingress-{ecsfargate_cluster_name}";

        // X-Ray: container definition
        public static string aws_xray__img_repo_name = "amazon/aws-xray-daemon";
        public static double aws_xray__container_port = 2000;

        // Container: general settings
        public static string container_nonroot_user_id = "1337";

        #region Service_A Attributes
        // Modernized/Service_A: Ingress traffic and health check entry port
        public static double modernizedService_A__app_container_listener_port = 80;

        // Modernized/Service_A: service definition
        public static string modernizedService_A__ecsfargate_service_name = $"{modernized_service_a_name}-definition";
        public static double modernizedService_A__ecsfargate_service_task_desired_count = 1;

        // Modernized/Service_A: task definition
        public static string modernizedService_A__ecsfargate_task_def_name = $"{modernized_service_a_name}-task-definition";
        public static double modernizedService_A__ecsfargate_task_def_memoryInMB = 512; // in MB
        public static double modernizedService_A__ecsfargate_task_def_cpuUnits = 256; // Translate to (0.25 vCPU): Navigate into the definition of the respective object's property and you will see options specified

        // Modernized/Service_A: container definition
        public static string modernizedService_A__container_name = $"{project_name}--{modernized_service_a_name}";
        public static string modernizedService_A__img_repo_name = "sharedcookie/modernized-service-a";
        #endregion

        #region Service_B Attributes
        // Modernized/Service_B: Ingress traffic and health check entry port
        public static double modernizedService_B__app_container_listener_port = 80;

        // Modernized/Service_B: service definition
        public static string modernizedService_B__ecsfargate_service_name = $"{modernized_service_b_name}-definition";
        public static double modernizedService_B__ecsfargate_service_task_desired_count = 1;

        // Modernized/Service_B: task definition
        public static string modernizedService_B__ecsfargate_task_def_name = $"{modernized_service_b_name}-task-definition";
        public static double modernizedService_B__ecsfargate_task_def_memoryInMB = 512; // in MB
        public static double modernizedService_B__ecsfargate_task_def_cpuUnits = 256; // Translate to (0.25 vCPU): Navigate into the definition of the respective object's property and you will see options specified

        // Modernized/Service_B: container definition
        public static string modernizedService_B__container_name = $"{project_name}--{modernized_service_b_name}";
        public static string modernizedService_B__img_repo_name = "sharedcookie/modernized-service-b";
        #endregion

        #endregion
    }
}
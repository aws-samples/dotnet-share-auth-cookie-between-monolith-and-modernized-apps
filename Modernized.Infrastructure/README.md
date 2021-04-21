# Infrastructure Anatomy

## Step #1: Pre-requisites

0. Ensure you've the AWS CLI installed and an AWS profile configured (secret id/key).
1. Ensure you've the AWS CDK CLI installed.
2. Ensure you've a custom domain and have the permissions to add 'A' records.  For example, http://monolith.example.com and http://modernized.example.com.
3. Ensure the modernized ServiceA and ServiceB docker images are available in the AWS Elastic Container Registry (ECR).  For Service* specific ECR repo name, please reference the 'InfrastructureConfigs.cs' file.
    - Each ```Modernized.Backend.Service*``` project had Dockerfile that you can use to build the image and upload to the ECR.
    > Before building the docker image, please ensure the <em>SharedCookieName<em> property in the appsettings.json file reflects your domain.
4. Create a SSM Parameter store access policy.

  > For demo only, the ```Reource: *``` policy scope works.  However, it's good practice to keep the policy's scope specific to a resource.  So feel free to adjust it.

```json
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Sid": "VisualEditor0",
            "Effect": "Allow",
            "Action": [
                "ssm:PutParameter",
                "ssm:GetParameter",
                "ssm:GetParameters"
            ],
            "Resource": "*"
        }
    ]
}
```

## Step #2: Deploy Monolith related resources

1. Using Visual Studio or AWS CLI, deploy the Monolith project to the AWS Elastic Beanstalk (EB).
    - Ensure, the 'Release' configuration is used, so the deployment picks up ```Web.Release.config``` changes.
    - Ensure, the following three environment variables (key/value) pairs are defined:
        - SharedCookieName:.example.com
        - ServiceA:Url: http://modernized.example.com/api/service-a
        - ServiceB:Url: http://modernized.example.com/api/service-b
2. Post successful deployment, locate the EB ```Service role``` and attach the ```SSM Parameter store access policy``` created earlier.
2. Using the default EB URL, validate the application loads successfully.
2. Add <em>monolith.example.com</em> sub-domain record pointing to the EB public IP (EC2 instance IP). 
3. Now, ensure you can access the application via the <em>monolith.example.com</em> friendly URL.

> Via this monolith UI, you can log-in and get a valid Auth cookie.

> Upon the first log-in process, the Data Protection implementation creates the AWS parameter store entry uploads a newly generated cookie encryption key.

## Step #3: Deploy Modernized related resources

1. From the CLI, run ```cdk deploy``` to create the following resources:

    - Landing network zone: VPC, Subnets, NAT, Internet Gateway, route tables etc.  For specific details, reference the ```InfrastructureStack.cs --> Networking Resources code region```
    - Application Load Balancer (ALB), listener/routes, and target.
    - ECS Fargate task/execution roles, cluster, service, and tasks to host ServiceA and ServiceB.
        - Register the Service* instances with the ALB targets.

2. Now, locate the ECS Fargate Task role and attach the <em>SSM Parameter store access policy</em> created earlier.
3. Add sub-domain, <em>modernized.example.com</em>, record pointing to the ALB DNS URL. 
4. Ensure you can access the application via your new URL.  Examples:
    - http://modernized.example.com/api/service-a <-- note: without a valid Auth cookie, you will get 401.
    - http://modernized.example.com/api/service-a/health <-- simple health check ping

    > You can pick any sub-domain name.  Please ensure, it matches with the EB environment variable targeting the Service*.

## Step #4 (Optional): Deploy API Gateway and Custom Authorizer related resources

1. Using Visual Studio or AWS CLI, deploy the Lambda function deployed above.
    - Attached the <em>SSM Parameter store access policy</em> to the Lambda role.
2. Via the AWS console, setup a API Gateway HTTP API.
3. Configure the API Gateway as below:
    - Ensure it's invocable via the http://modernized.example.com
    - For custom authorizer, use the Lambda function deployed above.
    - For integration, configure the route to pass the incoming traffic to the ALB managing the Service* (fronting ECS Fargate).

> The goal is to centralize the Auth cookie validation logic instead of having it be a part of every individual modernized microservice.

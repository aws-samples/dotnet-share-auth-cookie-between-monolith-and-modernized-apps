namespace Modernized.ApiGateway.LambdaAuthorizer.Error
{
    internal class UnauthorizedException : System.Exception
    {
        public UnauthorizedException() : base("Unauthorized")
        {
        }
    }
}

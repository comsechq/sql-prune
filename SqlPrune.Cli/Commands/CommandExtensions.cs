using System.CommandLine;

namespace Comsec.SqlPrune.Commands
{
    public static class CommandExtensions
    {
        public static Command AddAwsSdkCredentialsOptions(this Command command)
        {
            command.AddOption(new Option<string>("--profile",
                getDefaultValue: () => "",
                description: "The name of the AWS profile to use"));

            command.AddOption(new Option<string>("--profiles-location",
                getDefaultValue: () => "",
                description: "The path to the folder containing the AWS profiles"));
            
            command.AddOption(new Option<string>("--region",
                getDefaultValue: () => "",
                description: "The name of the AWS region to use"));

            return command;
        }
    }
}

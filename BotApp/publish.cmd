nuget restore
msbuild EchoBotWithCounter.sln -p:DeployOnBuild=true -p:PublishProfile=bot-service-app-Web-Deploy.pubxml -p:Password=tTS4bSDlweagSAgaJdMGJAGf8qW1BxGh5mimLKrk6jLEvkvtdHzAAhtBj6Hc


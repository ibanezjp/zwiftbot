nuget restore
msbuild CoreBot.sln -p:DeployOnBuild=true -p:PublishProfile=.\zwiftbot-Web-Deploy.pubxml -p:Password=tf1mZEXPdvNqPFaLDCAg8MM3pLfChovBLokke1jAg15L5tkLJngTW9NZoyA0


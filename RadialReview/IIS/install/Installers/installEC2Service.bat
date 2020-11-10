C:\Windows\System32\xcopy.exe /Y "c:\install\Files\ec2service\config.xml" "C:\Program Files\Amazon\Ec2ConfigService\Settings\config.xml"
C:\Windows\System32\xcopy.exe /Y "c:\install\Files\ec2service\AWS.EC2.Windows.CloudWatch.json" "C:\Program Files\Amazon\Ec2ConfigService\Settings\AWS.EC2.Windows.CloudWatch.json"

net stop ec2config
net start ec2config


%systemroot%\system32\inetsrv\appcmd.exe set config -section:system.applicationHost/applicationPools /[name='DefaultAppPool'].cpu.action:"KillW3wp" /commit:apphost
%systemroot%\system32\inetsrv\appcmd.exe set config -section:system.applicationHost/applicationPools /[name='DefaultAppPool'].cpu.resetInterval:"00:01:00" /commit:apphost
%systemroot%\system32\inetsrv\appcmd.exe set config -section:system.applicationHost/applicationPools /[name='DefaultAppPool'].cpu.limit:80000 /commit:apphost

%systemroot%\system32\inetsrv\appcmd.exe clear config -section:system.applicationHost/applicationPools "/[name='DefaultAppPool'].recycling.periodicRestart" /commit:apphost
%systemroot%\system32\inetsrv\appcmd.exe set config -section:system.applicationHost/applicationPools /[name='DefaultAppPool'].recycling.periodicRestart.time:"00:00:00" /commit:apphost
%systemroot%\system32\inetsrv\appcmd.exe set config -section:system.applicationHost/applicationPools "/+[name='DefaultAppPool'].recycling.periodicRestart.schedule.[@0,value='03:30:00']" /commit:apphost

%systemroot%\system32\inetsrv\appcmd.exe set config -section:system.applicationHost/applicationPools /[name='DefaultAppPool'].queueLength:10000 /commit:apphost
c:\\windows\\system32\\inetsrv\\appcmd.exe set config -section:system.applicationHost/applicationPools /[name='DefaultAppPool'].cpu.action:"KillW3wp" /commit:apphost
c:\\windows\\system32\\inetsrv\\appcmd.exe set config -section:system.applicationHost/applicationPools /[name='DefaultAppPool'].cpu.resetInterval:"00:10:00" /commit:apphost
c:\\windows\\system32\\inetsrv\\appcmd.exe set config -section:system.applicationHost/applicationPools /[name='DefaultAppPool'].cpu.limit:"89000" /commit:apphost
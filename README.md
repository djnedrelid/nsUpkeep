## NsUpkeep

En liten arkivert programvare fra 2018 laget for å holde navnetjenere aktive i windows systemer. 
F.eks. ved bruk av [OpenDNS sine skjold](https://www.opendns.com/home-internet-security/) 
eller [1.1.1.1 for Families](https://blog.cloudflare.com/introducing-1-1-1-1-for-families/) 
kan man på PCer hvert femte minutt automatisk håndheve at navnetjenerne fortsatt brukes på 
maskiner til ukyndige som kanskje kan ved uhell nullstille nettverkskortet og miste sine innstillinger, 
og dermed være utsatt igjen for uønskede nettsider/innhold. Programmet vil via WMI og 
Win32_NetworkAdapterConfiguration sjekke nettverksenheter som har IP bundet og aktiv, 
både kabel og trådløst. Test etter behov dersom det brukes flere nettverkskort/koblinger. 
Kopier filen til et endelig sted du vil ha den før bruk.  

```
C:\Users\dj\nslookup>NsUpkeep.exe --help

  _______          ____ ___         __
  \      \   _____|    |   \______ |  | __ ____   ____ ______
  /   |   \ /  ___/    |   /\____ \|  |/ // __ \_/ __ \\____ \
 /    |    \\___ \|    |  / |  |_> >    <\  ___/\  ___/|  |_> >
 \____|__  /____  >______/  |   __/|__|_ \\___  >\___  >   __/
         \/     \/          |__|        \/    \/     \/|__|
 https://thronic.com/Software/NsUpkeep/
 (C)2017-2018 Dag J Nedrelid <dj@thronic.com>
 Specified DNS server upkeep assistant.

 Options:
  --version
  --start-on-boot 8.8.8.8 8.8.4.4
  --remove-from-boot

  NOTE: --start-on-boot will use the current location,
        so put this file in appdata or something first.
        Do NOT use hostnames -- USE IP ADDRESSES!
```
==============================================================================
FLASH POS AVR BROKER - gmz.1.0.0. (May 2025)
==============================================================================


-------------------------------------------------------------------------
A. CONFIGURATION

1. Local Config

a. Application Settings

  <applicationSettings>
    <TkMqttBroker.WinService.Properties.FlashPosAvr>

      <setting name="PosApiKey" serializeAs="String"> = Api key to acces Pos Rest services. Encrypted

      <setting name="PosServiceUrl" serializeAs="String"> = Pos Rest service url, eg. http://localhost:55820

      <setting name="CameraPort" serializeAs="String"> = Common port where all flash cameras are listening, eg., 1884

      <setting name="PlateConfidenceMin" serializeAs="String"> = Min percentage confidence to accept plate, eg., 80 (80%)

      <setting name="MakeConfidenceMin" serializeAs="String"> = Min percentage confidence to accept make, eg., 80 (80%)

      <setting name="ColorConfidenceMin" serializeAs="String"> = Min percentage confidence to accept color, eg., 80 (80%)

      <setting name="StateConfidenceMin" serializeAs="String"> = Min percentage confidence to accept plate state, eg., 80 (80%)

    </TkMqttBroker.WinService.Properties.FlashPosAvr>
  </applicationSettings>

b. Net Tiers, as always

c. currentLocationGUID and currentWorkStationId as always
    - I guess we can assign a wkid for this app. Maybe FPA700? It is not much used, as of now


1. log4net. As always, to log in pos server db


1. Pos Policies
    - Ticketech NG Servicel Url
    - Ticketech NG Core Api
    - AVR Provider


1. Pos Devices
    - One device per camera in table LocationsMachinesConfigurations
    - model = AVRFlash
    - spoolerPrefix = ENTRY or EXIT, depending on which direction the camera is looking for
    - Eg., <posDevices> <device name="AVR" type="AVR" model="AVRFlash" required="false" location="10.30.50.106" spoolerPrefix="ENTRY" /></posDevices>

1. Suite Services
    - Add avr permits to list used to create the Core Api Key: write, read, edit , del:avr_entry


-------------------------------------------------------------------------
A. INSTALLING / RUNNING / QUERYING / STOPPING / DELETING

- Copy files into service folder (c:\pos\WinServices\FlashAvr)
- sc create TkFlashAvr binPath="c:\pos\WinServices\FlashAvr\TkFlashAvr.WinService.exe"
- sc start TkFlashAvr
- sc query TkFlashAvr
- sc stop TkFlashAvr
- sc delete TkFlashAvr

- Must have admin permits to run commands
- Service User must have access to folder


-------------------------------------------------------------------------

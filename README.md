[For English version, click here](#z21client-c-class)<br />
[Für die deutsche Version hier klicken](#z21client-c-klasse-deutsch)<br />
[Pour la version française, cliquez ici](#classe-C-z21client)

# Z21Client C# klasse

En C#-klasse til kommunikation med z21, z21Start, Z21 og Z21 XL centralstationerne til modeltogsbaner fra Roco/Fleischmann.

Z21Client-klassen understøtter følgende funktioner:

* Sprogversionering, dansk/tysk/fransk når sprog i Windows er sat til dansk/tysk/fransk. For alle andre sprog vises
  tekster på engelsk. Tysk og fransk er maskinelt oversat, og kan der kan derfor forekomme unøjagtigheder og
  fejloversættelser
* Forbindelse til Z21 via UDP
* Modtage information om lokomotiver (hastighed, retning, funktioner, protokol), når andre styreenheder bruges
* Modtage information om sporskifter/points (position, protokol), når andre styreenheder bruges
* Sende kommandoer til styring af lokomotiver (hastighed, retning, funktioner, protokol)
* Sende kommandoer til styring af sporskifter/points (position, protokol)
* Læse feedback fra Z21 (f.eks. lokomotivstatus)
* Understøttelse af flere lokomotiver
* Event-drevet arkitektur til håndtering af svar og opdateringer
* Asynkrone operationer for ikke-blokerende kommunikation
* Fejlhåndtering og genforbindelseslogik
* Understøttelse af protokoller brugt af z21/Z21 (DCC, Märklin Motorola)
* Logging-muligheder til fejlfinding og overvågning
* Z21Client er udviklet efter "AI Pair Programming" metoden

## Nyheder i denne version

* Ny event: Tilføjet eventen OnStatusChanged til Z21Client, som udløses, efter kald til den nye metode "GetStatus()"
* Eventen OnSystemStateChanged er ændret til at returnere status i den nye record-struktur
* Recorden SystemState er ændret til at anvende StatusChanged-recorden til information om CentralState
* Sprog: Tilføjet tekster på fransk
* Fejlrettelse: Efter connect, disconnect og efterfølgende reconnect blev nogle events ikke udløst korrekt.
  Dette er rettet ved at omskrive event-håndteringen i Z21Client
* Fejlrettelse: Firmware-workarounden til forkert rapportering af brugen
  af Märklin Motorola-protokollen er blevet justeret, da den blev anvendt
  på alle adresser, og den maksimale MM-adresse kan være 255 i version 2 af
  protokollen.

## z21 og z21Start låseinformation

Hvis din z21 eller z21Start er låst, kan du stadig sende kommandoer til den med denne klasse. Dog vil
kommandoerne blive ignoreret af z21/z21Start.

Hvis z21/z21Start er låst, kan du stadig bruge Z21Client-klassen til at skrive et overvågningsprogram, der læser
status for lokomotiver og sporskifter/points. Du kan også kalde metoder i Z21Client til at skifte protokol på 
lokomotiver og sporskifter/sporskiftedekodere. Du kan læse mere om hvilke metoder (der pakker z21 kommandoer ind) 
der kan kaldes når z21/z21Start er låst, i den officielle Z21 LAN Protcol dokomentation, på Z21s hjemmeside.

Bemærk, at da z21 (i hvidt kabinet) oprindeligt blev lanceret, var nogle låste og andre ulåste. For at låse
din z21 eller z21Start op, kan du købe en oplåsningskode:

* Roco varenummer 10814. Indeholder et trådløst access-point samt oplåsningskode til z21Start og z21 (hvidt kabinet)
* Roco varenummer 10818. Indeholder oplåsningskode til z21Start og z21 (hvidt kabinet)

Fra nu af vil betegnelsen Z21 blive brugt om alle fire versioner af Z21-familien af centralstationer. Hvis noget
kun gælder én af versionerne, vil det blive angivet.

Z21Client blev udviklet og testet ved brug af to z21Start centralstationer: én låst og én ulåst. Dette er grunden
til, at hverken LocoNet- eller CAN-bus-funktionalitet er implementeret i Z21Client-klassen.

Implementeringen er baseret på Roco-dokumentet *"Z21 LAN Protocol Specification"*, version 1.13 EN, dateret 6.
november 2023. Dokumentet kan downloades fra Z21-websitet.

## Fuldt funktionelt eksempelprojekt

For at se et eksempel på brugen af Z21Client, besøg venligst mit projekt *Z21Dashboard* på Github:

[https://github.com/J-Wachs/Z21Dashboard](https://github.com/J-Wachs/Z21Dashboard)

## Hvordan virker det?

Z21Client-klassen bruger UDP til at kommunikere med Z21-centralen. I din applikation skal du først
oprette forbindelse til Z21.

Da arkitekturen i Z21Client-klassen er event-drevet, skal du abonnere på de events, du ønsker at håndtere
i din applikation. For eksempel skal du abonnere på eventet *LocoStatusReceived* for at håndtere
opdateringer af lokomotivstatus.

De nødvendige ændringer af broadcast-flagene på Z21 bliver automatisk håndteret af Z21Client-klassen, når du
tilføjer din metode til Z21Client-eventet.

Eksempel på abonnement på LocoStatusReceived-eventet:

```csharp
...
@using IZ21Client Z21Client
...

...
Z21Client.LocoInfoReceived += OnLocoInfoReceived;
...

private async void OnLocoInfoReceived(object? sender, LocoInfo e)
{
	// Håndter modtaget lokomotivinfo
	Console.WriteLine($"Loco Info Received: Address={e.Address}, Speed={e.CurrentSpeed}, Direction={e.Direction}");
}
```

### Implementering af Märklin Motorola-protokol i Z21Client vs i Z21

Z21 understøtter både DCC og Märklin Motorola protokoller til styring af lokomotiver. Følgende versioner af
protokollerne er implementeret som følger:

* DCC, 14 trin: Protokol = DCC, hastighedstrin = 14
* DCC, 28 trin: Protokol = DCC, hastighedstrin = 28
* DCC, 128 trin: Protokol = DCC, hastighedstrin = 128
* Märklin Motorola 1, 14 trin: Protokol = Märklin Motorola, hastighedstrin = 14
* Märklin Motorola 2, 14 trin: Protokol = Märklin Motorola, hastighedstrin = 28
* Märklin Motorola 2, 28 trin: Protokol = Märklin Motorola, hastighedstrin = 128

På grund af dette rapporterer Z21 hastighedstrinene som 14, 28 eller 128, også når Märklin Motorola
benyttes. Z21Client er udviklet til at afspejle protokollen og hastighedstrinene, som man normalt ville forvente.
Derfor vil hastighedstrinene ved Märklin Motorola være hhv. 14, 14 eller 28.

Klassen *LocoInfo*, som bruges i Z21Client, afspejler denne implementering og indeholder to
hastigheds-egenskaber:

* **SpeedSteps:** Hastighedstrin som implementeret i Z21Client (DCC: 14, 28, 128; MM: 14, 14, 28)
* **NativeSpeedSteps:** Hastighedstrin som implementeret i Z21. Altid 14, 28 eller 128 – også for MM-protokollen.

### Ansvarsfraskrivelse: Implementering af ikke-dokumenteret 'Locomotive Slot Information'

Roco har i deres værktøj *Maintenance Tool* en mulighed for at se de 120 lokomotiv-slots, der findes i Z21. Men
den officielle *"Z21 LAN Protocol Specification"* dokumentation nævner ikke kommandoen og svaret til at læse disse slots.
Ved at overvåge dataudvekslingen mellem mine Z21 (to z21Start, én låst, én ulåst) kunne jeg se kommandoerne.
På grund af dette har jeg implementeret udokumenteret funktionalitet. Den virker i firmware 1.43
(en del af Maintenance Tool V1.18.3). Der gives ingen garanti for, at den vil virke i fremtidige firmwareudgaver.

## Workaround for Z21 firmware-fejl

I den seneste firmwareversion (1.43) for Z21-familien er der efter min vurdering en fejl. Når man eksplicit
forespørger lokomotivinformation (dvs. kalder Z21-kommandoen LAN_X_GET_LOCO_INFO, indkapslet i
Z21Client.GetLocoInfoAsync()), vil protokol-bitten i byte DB2 i svaret ikke blive sat for lokomotiver, der er
konfigureret til Märklin Motorola. Dog er protokol-bitten korrekt sat i events, der skyldes ændringer af lokomotivet
(f.eks. hastighed, retning, funktionstaster).

For at omgå denne fejl forespørger Z21Client protokollen for lokomotivet separat og leverer derefter korrekt
protokol i LocoInfoReceived-eventet.

## Installationsvejledning

### Hentning og afprøvning af Z21Client-klassen

Download repoet og opret et projekt, hvor du vil bruge Z21Client. Hvis du mangler inspiration, kan du se mit projekt
*Z21Dashboard* på Github:

[https://github.com/J-Wachs/Z21Dashboard](https://github.com/J-Wachs/Z21Dashboard)

### Opsætning af dit eget projekt til at bruge Z21Client-klassen

For at bruge Z21Client-klassen i dine egne projekter skal du tilføje komponentprojektet til din løsning. Derefter skal
du tilføje *Z21Client* til Program.cs eller MauiProgram.cs i dit projekt:

```csharp
...
// Tilføjet for Z21Client
builder.Services.AddSingleton<IZ21UdpClient, Z21UdpClient>();
builder.Services.AddSingleton<IZ21Client, Z21Client>();
// Slut
...
```

## Tilpasning af Z21Client til eget brug

Måske har du brug for flere oplysninger. Måske skal du bruge en konfigurationsværdi til nogle af de data, der returneres.
Måske har du brug for én af LocoNet- eller CAN-bus-kommandoerne/events.

Du er meget velkommen til at tilpasse en lokal version til dine behov.

## Fundet en fejl?

Opret venligst et issue i repoet.

## Kendte problemer (pågør)

Ingen på nuværende tidspunkt.

## FAQ

### Vil Z21Client fungere med Z21 START newGen?

På nuværende tidspunkt, har jeg ikke haft mulighed for at teste Z21Client med
Z21 START newGen, og jeg kan derfor ikke svare på om Z21Client virker med Z21
START newGen.

Ej heller er manualen "Z21 LAN Protocol" opdateret til at inkludere de
ændringer i Z21 protokollen, der måtte være nødvendige for Z21 START newGen.

### Når jeg kalder metoden QueryForZ21s vises min Z21 ikke

Listen som metoden returnerer er tom, og du får ingen fejl. Du kan forbinde med
Z21Client til din Z21 centralstation (alle modeller), og sende kommandoer og
modtage data. At metoden returnerer en tom liste, sker typisk når pc'en er
koblet på netværket trådløst.

For at finde Z21'ere på netværket udsender QueryForZ21s en UDP-broadcast som
Z21 centralstationerne skal svare på. Mange access points og routere blokerer
for UDP-broadcasts, og det er derfor muligt, at din Z21 ikke modtager
broadcastet og derfor ikke svarer på det. Det er også muligt, at din pc ikke
modtager svaret fra Z21.

Kik i opsætningen af dit access point eller router og se, om der er en
indstilling for at blokere for UDP-broadcasts. Hvis det er tilfældet, skal du
slå denne indstilling fra. Visse routere og access points har også en
indstilling for at blokere for UDP-broadcasts på det trådløse net alene. Andre
access points og routere har ikke en indstilling, men blokerer for
UDP-broadcasts på det trådløse net som standard. I dette tilfælde kan du prøve
at forbinde din pc til netværket med kabel for at se, om det løser problemet.
Hvis det gør det, er det sandsynligt, at dit access point eller router blokerer
for UDP-broadcasts på det trådløse net.

### Vil du implementere LocoNet- og CAN-bus-funktionalitet?

Det korte svar er nej. Det lange svar er, at jeg ikke ejer en Z21 eller Z21 XL,
derfor har jeg ikke behovet og kan ikke teste funktionaliteten.

### Vil du implementere understøttelse af trådløs forbindelse til Z21?

Faktisk – hvis dit netværk er konfigureret korrekt, og du har Roco 10814 eller
bruger dit eget access-point, kan du få trådløs adgang til Z21. Mit projekt
Z21Dashboard er testet over trådløst LAN, og det virker fint. Nogle gange
skulle jeg dog oprette forbindelse mere end én gang.

### Hvordan kommer jeg i gang med at skrive min egen applikation?

Tag et kig på Z21Client – særligt Z21Dashboard-applikationen – for at se,
hvordan den er implementeret og for inspiration til, hvad du selv kan lave.

## Liste over implementerede Z21 LAN Protokol-kommandoer
For at se en oversigt over implementerede Z21 LAN Protokol-kommandoer, se
tabellen ved at klikke [her](#implemented-z21-lan-protocol-commands).

<hr>

# Z21Client C# class
A C# class for communication with the z21, z21Start, Z21 and Z21 XL command stations for model railway layouts from Roco/Fleischmann.

The Z21Client class supports the following features:

* Language localization: Danish/German/French when the Windows language is set to Danish/German/French. For all
  other languages, texts are displayed in English. The German and French versions have been machine-translated, and
  may therefore contain inaccuracies or translation errors
* Connection to Z21 via UDP
* Receiving information about locomotives (speed, direction, functions, protocol) when other control devices are used
* Receiving information about turnouts/points (position, protocol) when other control devices are used
* Sending commands to control locomotives (speed, direction, functions, protocol)
* Sending commands to control turnouts/points (position, protocol)
* Reading feedback from Z21 (e.g. locomotive status)
* Support for multiple locomotives
* Event-driven architecture for handling responses and updates
* Asynchronous operations for non-blocking communication
* Error handling and reconnection logic
* Support for protocols used by z21/Z21 (DCC, Märklin Motorola)
* Logging options for troubleshooting and monitoring
* Z21Client was developed using the "AI Pair Programming" method

## What's new in this version

* New event: Added the OnStatusChanged event to Z21Client, which is triggered after a call to the new
  "GetStatus()" method
* The OnSystemStateChanged event has been changed to return the status using the new record structure
* The SystemState record has been changed to use the StatusChanged record for information about CentralState
* Bug fix: After connect, disconnect and subsequent reconnect, some events were not triggered correctly.
  This has been fixed by rewriting the event handling in Z21Client
* Language: Added French translations
* Bug fix: After connect, disconnect and subsequent reconnect, some events were not triggered correctly.
  This has been fixed by rewriting the event handling in Z21Client
* Bug fix: The firmware workaround for incorrect reporting of use
  of Märklin Motorola protocol has been adjusted, as it was applied
  to all addresses and the maximum MM address can be 255 in version 2 of the
  protocol

## z21 and z21Start lock information

If your z21 or z21Start is locked, you can still send commands to it using this class. However, the
commands will be ignored by the z21/z21Start.

If the z21/z21Start is locked, you can still use the Z21Client class to write a monitoring application that reads
the status of locomotives and turnouts/points. You can also call methods in Z21Client to change the protocol for
locomotives and turnouts/turnout decoders. You can read more about which methods (which encapsulate z21 commands)
can be called when the z21/z21Start is locked in the official Z21 LAN Protocol documentation on the Z21 website.

Note that when the z21 (in the white housing) was originally launched, some units were locked while others were unlocked. To unlock
your z21 or z21Start, you can purchase an unlock code:

* Roco item number 10814. Includes a wireless access point and an unlock code for z21Start and z21 (white housing)
* Roco item number 10818. Includes an unlock code for z21Start and z21 (white housing)

From now on, the designation Z21 will be used for all four versions of the Z21 family of command stations. If something
only applies to one of the versions, this will be stated.

Z21Client was developed and tested using two z21Start command stations: one locked and one unlocked. This is why neither LocoNet nor CAN bus functionality is implemented in the Z21Client class.

The implementation is based on the Roco document *"Z21 LAN Protocol Specification"*, version 1.13 EN, dated
6 November 2023. The document can be downloaded from the Z21 website.

## Fully functional example project

To see an example of how to use Z21Client, please visit my *Z21Dashboard* project on GitHub:

[https://github.com/J-Wachs/Z21Dashboard](https://github.com/J-Wachs/Z21Dashboard)

## How does it work?

The Z21Client class uses UDP to communicate with the Z21 command station. In your application, you must first
establish a connection to Z21.

Since the architecture of the Z21Client class is event-driven, you must subscribe to the events you want to handle
in your application. For example, you should subscribe to the *LocoStatusReceived* event to handle
locomotive status updates.

The required changes to the broadcast flags on Z21 are handled automatically by the Z21Client class when you
add your method to the Z21Client event.

Example of subscribing to the LocoStatusReceived event:

```csharp
...
@using IZ21Client Z21Client
...

Z21Client.LocoInfoReceived += OnLocoInfoReceived;
...

private async void OnLocoInfoReceived(object? sender, LocoInfo e)
{
	// Handle received locomotive information
	Console.WriteLine($"Loco Info Received: Address={e.Address}, Speed={e.CurrentSpeed}, Direction={e.Direction}");
}
```

### Implementation of the Märklin Motorola protocol in Z21Client vs. Z21

Z21 supports both DCC and Märklin Motorola protocols for controlling locomotives. The following versions of
the protocols are implemented as follows:

* DCC, 14 steps: Protocol = DCC, speed steps = 14
* DCC, 28 steps: Protocol = DCC, speed steps = 28
* DCC, 128 steps: Protocol = DCC, speed steps = 128
* Märklin Motorola 1, 14 steps: Protocol = Märklin Motorola, speed steps = 14
* Märklin Motorola 2, 14 steps: Protocol = Märklin Motorola, speed steps = 28
* Märklin Motorola 2, 28 steps: Protocol = Märklin Motorola, speed steps = 128

Because of this, Z21 reports the speed steps as 14, 28 or 128, even when Märklin Motorola
is being used. Z21Client is designed to reflect the protocol and speed steps as one would normally expect.
Therefore, for Märklin Motorola, the speed steps will be 14, 14 or 28, respectively.

The *LocoInfo* class used by Z21Client reflects this implementation and contains two
speed step properties:

* **SpeedSteps:** Speed steps as implemented in Z21Client (DCC: 14, 28, 128; MM: 14, 14, 28)
* **NativeSpeedSteps:** Speed steps as implemented in Z21. Always 14, 28 or 128 – also for the MM protocol.

### Disclaimer: Implementation of undocumented 'Locomotive Slot Information'

Roco's *Maintenance Tool* provides an option to view the 120 locomotive slots available in Z21. However,
the official *"Z21 LAN Protocol Specification"* documentation does not mention the command and response used to read these slots.
By monitoring the data exchange between my Z21 units (two z21Start units, one locked and one unlocked), I was able to observe the commands.
As a result, I have implemented undocumented functionality. It works with firmware 1.43
(part of Maintenance Tool V1.18.3). No guarantee is given that it will work with future firmware versions.

## Workaround for Z21 firmware bug

In my assessment, there is a bug in the latest firmware version (1.43) for the Z21 family. When explicitly
requesting locomotive information (i.e. calling the Z21 command LAN_X_GET_LOCO_INFO, encapsulated in
Z21Client.GetLocoInfoAsync()), the protocol bit in byte DB2 in the response is not set for locomotives
configured for Märklin Motorola. However, the protocol bit is correctly set in events caused by changes to the locomotive
(e.g. speed, direction, function keys).

To work around this bug, Z21Client requests the protocol for the locomotive separately and then provides the correct
protocol in the LocoInfoReceived event.

## Installation guide

### Downloading and testing the Z21Client class

Download the repository and create a project in which you want to use Z21Client. If you need some inspiration, you can look at my project
*Z21Dashboard* on GitHub:

[https://github.com/J-Wachs/Z21Dashboard](https://github.com/J-Wachs/Z21Dashboard)

### Configuring your own project to use the Z21Client class

To use the Z21Client class in your own projects, you must add the component project to your solution. Then
you must add *Z21Client* to Program.cs or MauiProgram.cs in your project:

```csharp
...
// Added for Z21Client
builder.Services.AddSingleton<IZ21UdpClient, Z21UdpClient>();
builder.Services.AddSingleton<IZ21Client, Z21Client>();
// End
```

## Customizing Z21Client for your own use

Maybe you need more information. Maybe you need a configuration value for some of the data returned.
Maybe you need one of the LocoNet or CAN bus commands/events.

You are very welcome to customize a local version to suit your needs.

## Found a bug?

Please create an issue in the repository.

## Known issues (current)

None at this time.

## FAQ

### Will Z21Client work with Z21 START newGen?

At present, I have not had the opportunity to test Z21Client with
Z21 START newGen, and therefore I cannot say whether Z21Client works
with Z21 START newGen.

Nor has the "Z21 LAN Protocol" manual been updated to include any changes
to the Z21 protocol that may be necessary for Z21 START newGen.

### Can I use Z21Client with my Digikeijs DR5000?

Yes, you can. However, this requires that you have set the network
protocol to "Z21" in the DR5000's settings. You cannot use Z21Client
with the DR5000 if you have selected other protocols.

Z21Client has been tested with a DR5000 running firmware 1.6.3, and
it works fine.

Please note that the Z21 command stations are multiprotocol devices,
whereas the DR5000 is exclusively a DCC command station. Since DR5000
firmware 1.6.3, when using the Z21 protocol, reports itself as a Z21
(with a black housing) with firmware version 1.29, it cannot be queried
about which protocols are enabled. You will therefore see both DCC and
Märklin Motorola protocols in Z21Client. Selecting the Märklin
Motorola protocol for locomotives or turnouts will have no effect.

### When I call the QueryForZ21s method, my Z21 does not appear

The list returned by the method is empty, and you receive no error. You can connect to your Z21 command station (all
models) using Z21Client and send commands and receive data. The method returning an empty list typically happens when the PC is connected to
the network wirelessly.

To find Z21 units on the network, QueryForZ21s sends a UDP broadcast to which the Z21 command stations must respond. Many
access points and routers block UDP broadcasts, which means that your Z21 may not receive the broadcast and
therefore does not respond to it. It is also possible that your PC does not receive the response from the Z21.

Check the configuration of your access point or router to see whether there is a setting for blocking UDP broadcasts.
If so, disable this setting. Some routers and access points also have a setting for blocking UDP broadcasts on the wireless
network only. Other access points and routers do not have such a setting but block UDP broadcasts on the wireless network by default. In this case, you can try connecting your PC to the network using a cable
to see whether this solves the problem. If it does, it is likely that your access point or router is blocking
UDP broadcasts on the wireless network.

### Do you want to implement LocoNet and CAN bus functionality?

The short answer is no. The long answer is that I do not own a Z21 or Z21 XL, so I do not need the functionality and cannot
test it.

### Do you want to implement support for a wireless connection to Z21?

Actually – if your network is configured correctly, and you have Roco 10814 or use your own access point, you can
access Z21 wirelessly. My Z21Dashboard project has been tested over wireless LAN, and it works fine. Sometimes,
however, I had to establish the connection more than once.

### How do I get started writing my own application?

Take a look at Z21Client – especially the Z21Dashboard application – to see how it is implemented and
for inspiration for what you can create yourself.

## List of implemented Z21 LAN Protocol commands

To see an overview of the implemented Z21 LAN Protocol commands, see the table by clicking [here](#implemented-z21-lan-protocol-commands).

<hr>

# Z21Client C# Klasse, Deutsch

Eine C#-Klasse zur Kommunikation mit den Zentralen z21, z21Start, Z21 und Z21 XL von Roco/Fleischmann für
Modelleisenbahnanlagen.

Die Klasse Z21Client unterstützt folgende Funktionen:

* Sprachlokalisierung: Dänisch/Deutsch/Französisch, wenn die Windows-Sprache auf Dänisch, Deutsch oder Französisch
  eingestellt ist. Bei allen anderen Sprachen werden die Texte auf Englisch angezeigt. Die deutsche und französische
  Version wurden maschinell übersetzt und können daher Ungenauigkeiten oder Übersetzungsfehler enthalten
* Verbindung zur Z21 über UDP
* Empfang von Informationen über Lokomotiven (Geschwindigkeit, Fahrtrichtung, Funktionen, Protokoll), wenn andere
  Steuergeräte verwendet werden
* Empfang von Informationen über Weichen (Stellung, Protokoll), wenn andere Steuergeräte verwendet werden
* Senden von Befehlen zur Steuerung von Lokomotiven (Geschwindigkeit, Fahrtrichtung, Funktionen, Protokoll)
* Senden von Befehlen zur Steuerung von Weichen (Stellung, Protokoll)
* Auslesen von Rückmeldungen der Z21 (z. B. Lokomotivstatus)
* Unterstützung mehrerer Lokomotiven
* Ereignisgesteuerte Architektur zur Verarbeitung von Antworten und Aktualisierungen
* Asynchrone Operationen für eine nicht blockierende Kommunikation
* Fehlerbehandlung und automatische Wiederverbindung
* Unterstützung der von z21/Z21 verwendeten Protokolle (DCC, Märklin Motorola)
* Logging-Optionen zur Fehlersuche und Überwachung
* Z21Client wurde nach der Methode „AI Pair Programming“ entwickelt

## Was ist neu in dieser Version?

* Neues Ereignis: Das Ereignis `OnStatusChanged` wurde zu Z21Client hinzugefügt. Es wird nach einem Aufruf der neuen
  Methode `GetStatus()` ausgelöst.
* Das Ereignis `OnSystemStateChanged` liefert den Status jetzt über die neue Record-Struktur zurück.
* Der Record `SystemState` verwendet jetzt die Struktur `StatusChanged` für Informationen über den Zustand der
  Zentrale (`CentralState`).
* Sprache: Französische Übersetzungen wurden hinzugefügt.
* Fehlerbehebung: Nach einer Verbindung, einer Trennung und anschließendem erneuten Verbinden wurden einige
  Ereignisse nicht korrekt ausgelöst. Dies wurde durch eine Überarbeitung der Ereignisbehandlung in Z21Client behoben.
* Fehlerbehebung: Die Firmware-Workaround-Lösung für die fehlerhafte
  Erkennung der Verwendung des Märklin-Motorola-Protokolls wurde angepasst,
  da sie auf alle Adressen angewendet wurde und die maximale MM-Adresse in
  Version 2 des Protokolls 255 sein kann.

## Informationen zur Sperrung von z21 und z21Start

Wenn Ihre z21 oder z21Start gesperrt ist, können Sie mit dieser Klasse weiterhin Befehle an sie senden. Die Befehle
werden von der z21/z21Start jedoch ignoriert.

Auch bei einer gesperrten z21/z21Start können Sie die Klasse Z21Client verwenden, um eine Überwachungsanwendung zu
erstellen, die den Status von Lokomotiven und Weichen ausliest. Sie können außerdem Methoden von Z21Client aufrufen, um das Protokoll für Lokomotiven sowie Weichen- bzw. Weichendecoder zu ändern.

Welche Methoden, die Z21-Befehle kapseln, bei einer gesperrten z21/z21Start aufgerufen werden können, ist in der
offiziellen Dokumentation **„Z21 LAN Protocol“** auf der Z21-Website beschrieben.

Als die z21 (im weißen Gehäuse) ursprünglich auf den Markt kam, waren einige Geräte gesperrt und andere nicht. Um
Ihre z21 oder z21Start zu entsperren, können Sie einen Freischaltcode erwerben:

* Roco Artikelnummer 10814. Enthält einen WLAN-Access-Point und einen Freischaltcode für z21Start und z21 (weißes
  Gehäuse).
* Roco Artikelnummer 10818. Enthält einen Freischaltcode für z21Start und z21 (weißes Gehäuse).

Ab sofort wird die Bezeichnung **Z21** für alle vier Varianten der Z21-Produktfamilie verwendet. Wenn etwas nur für
eine bestimmte Variante gilt, wird dies ausdrücklich angegeben.

Z21Client wurde mit zwei z21Start-Zentralen entwickelt und getestet: einer gesperrten und einer nicht gesperrten. Aus
diesem Grund sind in der Klasse Z21Client weder LocoNet- noch CAN-Bus-Funktionen implementiert.

Die Implementierung basiert auf dem Dokument **„Z21 LAN Protocol Specification“**, Version 1.13 EN vom 6. November
2023. Das Dokument kann von der Z21-Website heruntergeladen werden.

## Vollständig funktionsfähiges Beispielprojekt

Ein Beispiel für die Verwendung von Z21Client finden Sie im GitHub-Projekt **Z21Dashboard**:

https://github.com/J-Wachs/Z21Dashboard

## Wie funktioniert es?

Die Klasse Z21Client verwendet UDP zur Kommunikation mit der Z21-Zentrale. In Ihrer Anwendung müssen Sie zunächst
eine Verbindung zur Z21 herstellen.

Da die Architektur von Z21Client ereignisgesteuert ist, müssen Sie die Ereignisse abonnieren, die Sie in Ihrer
Anwendung verarbeiten möchten. Beispielsweise können Sie das Ereignis `LocoInfoReceived` abonnieren, um
Statusänderungen von Lokomotiven zu verarbeiten.

Die erforderlichen Änderungen an den Broadcast-Flags der Z21 werden von der Klasse Z21Client automatisch vorgenommen,
sobald Sie Ihre Methode beim entsprechenden Ereignis registrieren.

Beispiel für das Abonnieren des Ereignisses `LocoStatusReceived`:

```csharp
...
@using IZ21Client Z21Client
...

Z21Client.LocoInfoReceived += OnLocoInfoReceived;
...

private async void OnLocoInfoReceived(object? sender, LocoInfo e)
{
    // Empfangene Lokomotivinformationen verarbeiten
    Console.WriteLine($"Lok-Info empfangen: Adresse={e.Address}, Geschwindigkeit={e.CurrentSpeed}, Fahrtrichtung={e.Direction}");
}
```

### Implementierung des Märklin-Motorola-Protokolls in Z21Client im Vergleich zur Z21

Die Z21 unterstützt sowohl DCC als auch Märklin Motorola zur Steuerung von Lokomotiven. Die folgenden
Protokollvarianten sind implementiert:

* DCC, 14 Fahrstufen: Protokoll = DCC, Fahrstufen = 14
* DCC, 28 Fahrstufen: Protokoll = DCC, Fahrstufen = 28
* DCC, 128 Fahrstufen: Protokoll = DCC, Fahrstufen = 128
* Märklin Motorola 1, 14 Fahrstufen: Protokoll = Märklin Motorola, Fahrstufen = 14
* Märklin Motorola 2, 14 Fahrstufen: Protokoll = Märklin Motorola, Fahrstufen = 28
* Märklin Motorola 2, 28 Fahrstufen: Protokoll = Märklin Motorola, Fahrstufen = 128

Aus diesem Grund meldet die Z21 die Fahrstufen mit 14, 28 oder 128, auch wenn das Märklin-Motorola-Protokoll
verwendet wird. Z21Client ist so ausgelegt, dass Protokoll und Fahrstufen so dargestellt werden, wie man es
normalerweise erwarten würde.

Daher werden beim Märklin-Motorola-Protokoll die Fahrstufen entsprechend als 14, 14 bzw. 28 angegeben.

Die von Z21Client verwendete Klasse `LocoInfo` enthält deshalb zwei Eigenschaften für die Fahrstufen:

* **SpeedSteps:** Fahrstufen, wie sie von Z21Client verwendet werden (DCC: 14, 28, 128; MM: 14, 14, 28)
* **NativeSpeedSteps:** Fahrstufen, wie sie von der Z21 verwendet werden. Immer 14, 28 oder 128 – auch beim
* MM-Protokoll.

## Hinweis zur Implementierung der undokumentierten „Lokomotiv-Slot-Information“

Das **Roco Maintenance Tool** bietet die Möglichkeit, die 120 in der Z21 verfügbaren Lokomotiv-Slots anzuzeigen. Die
offizielle Dokumentation **„Z21 LAN Protocol Specification“** beschreibt jedoch weder den Befehl noch die Antwort,
die zum Auslesen dieser Slots verwendet werden.

Durch die Überwachung des Datenaustauschs zwischen meinen Z21-Geräten (zwei z21Start-Zentralen, eine gesperrt und
eine nicht gesperrt) konnte ich die entsprechenden Befehle beobachten.

Auf dieser Grundlage habe ich diese undokumentierte Funktionalität implementiert. Sie funktioniert mit Firmware 1.43
(Bestandteil des Maintenance Tools V1.18.3). Es wird jedoch keine Garantie übernommen, dass sie mit zukünftigen
Firmware-Versionen funktioniert.

## Workaround für einen Z21-Firmwarefehler

Nach meiner Einschätzung enthält die aktuelle Firmware-Version 1.43 der Z21-Produktfamilie einen Fehler.

Wenn Lokomotivinformationen explizit angefordert werden (also durch Aufruf des Z21-Befehls `LAN_X_GET_LOCO_INFO`,
gekapselt in `Z21Client.GetLocoInfoAsync()`), wird das Protokollbit in Byte DB2 der Antwort bei Lokomotiven, die für
Märklin Motorola konfiguriert sind, nicht gesetzt.

Bei Ereignissen, die durch Änderungen an der Lokomotive ausgelöst werden (z. B. Geschwindigkeit, Fahrtrichtung oder
Funktionstasten), wird das Protokollbit hingegen korrekt gesetzt.

Als Workaround für diesen Fehler fordert Z21Client das Protokoll der Lokomotive separat an und stellt anschließend im
Ereignis `LocoInfoReceived` das korrekte Protokoll bereit.

## Installationsanleitung

### Z21Client herunterladen und testen

Laden Sie das Repository herunter und erstellen Sie ein Projekt, in dem Sie Z21Client verwenden möchten.

Wenn Sie Inspiration benötigen, können Sie sich mein Projekt **Z21Dashboard** auf GitHub ansehen:

https://github.com/J-Wachs/Z21Dashboard

### Z21Client in einem eigenen Projekt konfigurieren

Um Z21Client in Ihrem eigenen Projekt zu verwenden, müssen Sie das Component-Projekt zu Ihrer Solution hinzufügen.
Anschließend müssen Sie `Z21Client` in `Program.cs` oder `MauiProgram.cs` registrieren:

```csharp
...
// Für Z21Client hinzugefügt
builder.Services.AddSingleton<IZ21UdpClient, Z21UdpClient>();
builder.Services.AddSingleton<IZ21Client, Z21Client>();
// Ende
```

## Z21Client für die eigene Anwendung anpassen

Vielleicht benötigen Sie zusätzliche Informationen. Vielleicht benötigen Sie einen Konfigurationswert für einige der
zurückgegebenen Daten. Oder vielleicht benötigen Sie Befehle bzw. Ereignisse für LocoNet oder den CAN-Bus.

Sie können jederzeit eine lokale Version von Z21Client an Ihre eigenen Anforderungen anpassen.

## Einen Fehler gefunden?

Bitte erstellen Sie ein Issue im Repository.

## Bekannte Probleme (aktuell)

Keine.

## FAQ

### Wird Z21Client mit Z21 START newGen funktionieren?

Derzeit hatte ich noch keine Möglichkeit, Z21Client mit der
Z21 START newGen zu testen. Daher kann ich nicht sagen, ob Z21Client
mit der Z21 START newGen funktioniert.

Auch das Handbuch "Z21 LAN Protocol" wurde noch nicht aktualisiert, um
die Änderungen am Z21-Protokoll aufzunehmen, die für die
Z21 START newGen möglicherweise erforderlich sind.

### Kann ich Z21Client mit meiner Digikeijs DR5000 verwenden?

Ja, das können Sie. Voraussetzung ist jedoch, dass Sie in den
Einstellungen der DR5000 das Netzwerkprotokoll auf "Z21" eingestellt
haben. Wenn Sie ein anderes Protokoll ausgewählt haben, können Sie
Z21Client nicht mit der DR5000 verwenden.

Z21Client wurde mit einer DR5000 mit Firmware 1.6.3 getestet und
funktioniert einwandfrei.

Bitte beachten Sie, dass die Z21-Zentralen Multiprotokoll-Geräte sind,
während die DR5000 ausschließlich eine DCC-Zentrale ist. Da sich die
DR5000-Firmware 1.6.3 im Z21-Protokoll als eine Z21 (im schwarzen
Gehäuse) mit der Firmware-Version 1.29 meldet, kann nicht abgefragt
werden, welche Protokolle aktiviert sind. Daher werden in Z21Client
sowohl DCC als auch Märklin-Motorola-Protokolle angezeigt. Die Auswahl
des Märklin-Motorola-Protokolls für Lokomotiven oder Weichen hat keine
Auswirkung.

### Wenn ich die Methode `QueryForZ21s` aufrufe, wird meine Z21 nicht gefunden

Die von der Methode zurückgegebene Liste ist leer und es wird kein Fehler gemeldet.

Sie können sich mit Ihrer Z21-Zentrale verbinden (alle Modelle) und mit Z21Client Befehle senden sowie Daten
empfangen. Dass die Methode eine leere Liste zurückgibt, tritt typischerweise auf, wenn der PC drahtlos mit dem
Netzwerk verbunden ist.

Um Z21-Geräte im Netzwerk zu finden, sendet `QueryForZ21s` einen UDP-Broadcast. Die Z21-Zentralen müssen auf diesen
Broadcast antworten. Viele Access Points und Router blockieren UDP-Broadcasts. Dadurch empfängt die Z21 den Broadcast
möglicherweise nicht und antwortet daher nicht. Es ist auch möglich, dass Ihr PC die Antwort der Z21 nicht empfängt.

Überprüfen Sie die Konfiguration Ihres Access Points oder Routers und stellen Sie sicher, dass UDP-Broadcasts nicht
blockiert werden.

Einige Router und Access Points verfügen außerdem über eine Einstellung, mit der UDP-Broadcasts ausschließlich im
WLAN blockiert werden können. Andere Geräte bieten eine solche Einstellung nicht, blockieren UDP-Broadcasts im WLAN
jedoch standardmäßig.

In diesem Fall können Sie versuchen, Ihren PC per Netzwerkkabel mit dem Netzwerk zu verbinden. Wenn dies das Problem
behebt, blockiert Ihr Access Point oder Router wahrscheinlich UDP-Broadcasts im WLAN.

### Möchten Sie LocoNet- und CAN-Bus-Funktionen implementieren?

Die kurze Antwort lautet: nein.

Die ausführliche Antwort lautet: Ich besitze keine Z21 oder Z21 XL und benötige diese Funktionen daher nicht.
Außerdem könnte ich die Implementierung nicht testen.

### Möchten Sie Unterstützung für eine drahtlose Verbindung zur Z21 implementieren?

Eigentlich ist dies nicht erforderlich. Wenn Ihr Netzwerk korrekt konfiguriert ist und Sie Roco 10814 oder einen
eigenen Access Point verwenden, können Sie drahtlos auf die Z21 zugreifen.

Mein Projekt Z21Dashboard wurde über WLAN getestet und funktioniert problemlos. Manchmal musste ich die Verbindung
allerdings mehr als einmal herstellen.

### Wie beginne ich mit der Entwicklung meiner eigenen Anwendung?

Sehen Sie sich Z21Client und insbesondere die Anwendung Z21Dashboard an, um zu verstehen, wie die Implementierung 
funktioniert und welche Möglichkeiten Ihnen für eigene Projekte zur Verfügung stehen.

## Liste der implementierten Z21-LAN-Protokollbefehle

Eine Übersicht über die in Z21Client implementierten Befehle des Z21-LAN-Protokolls finden Sie in der Tabelle unter
**„Implementierte Z21-LAN-Protokollbefehle“**.

<hr>

# Classe C# Z21Client

Une classe C# permettant de communiquer avec les centrales z21, z21Start, Z21 et Z21 XL de Roco/Fleischmann pour les
réseaux de modélisme ferroviaire.

La classe Z21Client prend en charge les fonctionnalités suivantes :

* Localisation linguistique : danois/allemand/français lorsque la langue de Windows est définie sur le danois,
  l’allemand ou le français. Pour toutes les autres langues, les textes sont affichés en anglais. Les versions
  allemande et française ont été traduites automatiquement et peuvent donc contenir des imprécisions ou des erreurs
  de traduction
* Connexion à la Z21 via UDP
* Réception des informations concernant les locomotives (vitesse, sens de marche, fonctions, protocole) lorsque
  d’autres appareils de commande sont utilisés
* Réception des informations concernant les aiguillages (position, protocole) lorsque d’autres appareils de commande
  sont utilisés
* Envoi de commandes pour contrôler les locomotives (vitesse, sens de marche, fonctions, protocole)
* Envoi de commandes pour contrôler les aiguillages (position, protocole)
* Lecture des informations de retour de la Z21 (par exemple, l’état d’une locomotive)
* Prise en charge de plusieurs locomotives
* Architecture événementielle pour le traitement des réponses et des mises à jour
* Opérations asynchrones pour une communication non bloquante
* Gestion des erreurs et logique de reconnexion
* Prise en charge des protocoles utilisés par z21/Z21 (DCC, Märklin Motorola)
* Options de journalisation pour le dépannage et la surveillance
* Z21Client a été développé selon la méthode « AI Pair Programming »

## Nouveautés de cette version

* Nouvel événement : l’événement `OnStatusChanged` a été ajouté à Z21Client. Il est déclenché après un appel à la
  nouvelle méthode `GetStatus()`.
* L’événement `OnSystemStateChanged` renvoie désormais l’état au moyen de la nouvelle structure `record`.
* Le `record` `SystemState` utilise désormais la structure `StatusChanged` pour les informations concernant l’état de
  la centrale (`CentralState`).
* Langue : ajout des traductions françaises.
* Correction d’un bug : après une connexion, une déconnexion puis une reconnexion, certains événements n’étaient pas
  correctement déclenchés. Ce problème a été corrigé en réécrivant la gestion des événements dans Z21Client.
* Correction de bug : le contournement (« workaround ») du firmware concernant
  la détection incorrecte de l’utilisation du protocole Märklin Motorola a été
  ajusté, car il était appliqué à toutes les adresses, alors que l’adresse MM
  maximale peut être 255 dans la version 2 du protocole.

## Informations concernant le verrouillage de la z21 et de la z21Start

Si votre z21 ou z21Start est verrouillée, vous pouvez toujours lui envoyer des commandes à l’aide de cette classe.
Toutefois, les commandes seront ignorées par la z21/z21Start.

Même lorsque la z21/z21Start est verrouillée, vous pouvez utiliser la classe Z21Client pour créer une application de
surveillance permettant de lire l’état des locomotives et des aiguillages. Vous pouvez également appeler les méthodes
de Z21Client permettant de modifier le protocole utilisé pour les locomotives ainsi que pour les aiguillages et leurs
décodeurs.

Vous trouverez plus d’informations sur les méthodes, qui encapsulent les commandes Z21 pouvant être appelées lorsque
la z21/z21Start est verrouillée, dans la documentation officielle **« Z21 LAN Protocol »** disponible sur le site Web
de Z21.

Lors de la commercialisation initiale de la z21 (boîtier blanc), certaines unités étaient verrouillées tandis que
d’autres ne l’étaient pas. Pour déverrouiller votre z21 ou z21Start, vous pouvez acheter un code de déverrouillage:

* Roco référence 10814. Comprend un point d’accès sans fil ainsi qu’un code de déverrouillage pour z21Start et z21
  (boîtier blanc).
* Roco référence 10818. Comprend un code de déverrouillage pour z21Start et z21 (boîtier blanc).

À partir de maintenant, la désignation **Z21** sera utilisée pour les quatre versions de la famille Z21. Si une
information ne concerne qu’une version particulière, cela sera indiqué explicitement.

Z21Client a été développé et testé avec deux centrales z21Start : une verrouillée et une déverrouillée. C’est
pourquoi aucune fonctionnalité LocoNet ou CAN bus n’est implémentée dans la classe Z21Client.

L’implémentation est basée sur le document Roco **« Z21 LAN Protocol Specification »**, version 1.13 EN, daté du 6
novembre 2023. Ce document peut être téléchargé depuis le site Web de Z21.

## Projet d’exemple entièrement fonctionnel

Pour voir un exemple d’utilisation de Z21Client, consultez mon projet **Z21Dashboard** sur GitHub:

https://github.com/J-Wachs/Z21Dashboard

## Comment cela fonctionne-t-il ?

La classe Z21Client utilise UDP pour communiquer avec la centrale Z21. Dans votre application, vous devez d’abord
établir une connexion avec la Z21.

Comme l’architecture de Z21Client est basée sur les événements, vous devez vous abonner aux événements que vous
souhaitez traiter dans votre application. Par exemple, vous pouvez vous abonner à l’événement `LocoInfoReceived` afin
de traiter les mises à jour de l’état des locomotives.

Les modifications nécessaires des indicateurs de diffusion (« broadcast flags ») de la Z21 sont gérées
automatiquement par la classe Z21Client lorsque vous abonnez votre méthode à l’événement Z21Client correspondant.

Exemple d’abonnement à l’événement `LocoStatusReceived` :

```csharp
...
@using IZ21Client Z21Client
...

Z21Client.LocoInfoReceived += OnLocoInfoReceived;
...

private async void OnLocoInfoReceived(object? sender, LocoInfo e)
{
    // Traiter les informations reçues concernant la locomotive
    Console.WriteLine($"Informations locomotive reçues : Adresse={e.Address}, Vitesse={e.CurrentSpeed}, Sens={e.Direction}");
}
```

### Implémentation du protocole Märklin Motorola dans Z21Client par rapport à la Z21

La Z21 prend en charge les protocoles DCC et Märklin Motorola pour la commande des locomotives. Les versions
suivantes des protocoles sont implémentées:

* DCC, 14 crans : Protocole = DCC, crans de vitesse = 14
* DCC, 28 crans : Protocole = DCC, crans de vitesse = 28
* DCC, 128 crans : Protocole = DCC, crans de vitesse = 128
* Märklin Motorola 1, 14 crans : Protocole = Märklin Motorola, crans de vitesse = 14
* Märklin Motorola 2, 14 crans : Protocole = Märklin Motorola, crans de vitesse = 28
* Märklin Motorola 2, 28 crans : Protocole = Märklin Motorola, crans de vitesse = 128

Pour cette raison, la Z21 indique les crans de vitesse comme étant 14, 28 ou 128, même lorsque le protocole Märklin
Motorola est utilisé.

Z21Client est conçu pour présenter le protocole et le nombre de crans de vitesse de la manière normalement attendue.
Ainsi, pour le protocole Märklin Motorola, le nombre de crans de vitesse est respectivement de 14, 14 ou 28.

La classe `LocoInfo` utilisée par Z21Client contient donc deux propriétés relatives aux crans de vitesse:

* **SpeedSteps** : nombre de crans de vitesse tel qu’implémenté dans Z21Client (DCC : 14, 28, 128 ; MM : 14, 14, 28)
* **NativeSpeedSteps** : nombre de crans de vitesse tel qu’implémenté dans la Z21. Toujours 14, 28 ou 128, y compris
  avec le protocole MM.

## Avertissement : implémentation des « informations sur les slots de locomotives » non documentées

Le **Maintenance Tool** de Roco permet d’afficher les 120 slots de locomotives disponibles dans la Z21. Cependant, la
documentation officielle **« Z21 LAN Protocol Specification »** ne décrit ni la commande ni la réponse utilisées pour
lire ces slots.

En surveillant les échanges de données entre mes appareils Z21 (deux centrales z21Start, l’une verrouillée et l’autre
déverrouillée), j’ai pu observer les commandes correspondantes.

J’ai ainsi implémenté cette fonctionnalité non documentée. Elle fonctionne avec le firmware 1.43 (inclus dans le
Maintenance Tool V1.18.3). Aucune garantie n’est toutefois donnée quant à son fonctionnement avec les futures
versions du firmware.

## Contournement d’un bug du firmware Z21

Selon mon analyse, la dernière version du firmware (1.43) de la famille Z21 contient un bug.

Lorsqu’on demande explicitement les informations d’une locomotive, c’est-à-dire lors de l’appel de la commande Z21
`LAN_X_GET_LOCO_INFO`, encapsulée dans `Z21Client.GetLocoInfoAsync()`, le bit de protocole dans l’octet DB2 de la
réponse n’est pas défini pour les locomotives configurées avec le protocole Märklin Motorola.

En revanche, le bit de protocole est correctement défini dans les événements déclenchés par des modifications de la
locomotive, par exemple lors d’un changement de vitesse, de sens de marche ou de l’utilisation des touches de fonction.

Pour contourner ce problème, Z21Client demande séparément le protocole de la locomotive, puis fournit le protocole 
correct dans l’événement `LocoInfoReceived`.

## Guide d’installation

### Télécharger et tester la classe Z21Client

Téléchargez le dépôt et créez un projet dans lequel vous souhaitez utiliser Z21Client.

Si vous avez besoin d’inspiration, vous pouvez consulter mon projet **Z21Dashboard** sur GitHub :

https://github.com/J-Wachs/Z21Dashboard

### Configurer Z21Client dans votre propre projet

Pour utiliser Z21Client dans votre propre projet, vous devez ajouter le projet Component à votre solution. Vous devez
ensuite enregistrer `Z21Client` dans `Program.cs` ou `MauiProgram.cs`:

```csharp
...
// Ajouté pour Z21Client
builder.Services.AddSingleton<IZ21UdpClient, Z21UdpClient>();
builder.Services.AddSingleton<IZ21Client, Z21Client>();
// Fin
```

## Personnaliser Z21Client pour votre propre utilisation

Vous avez peut-être besoin d’informations supplémentaires. Vous avez peut-être besoin d’une valeur de configuration
pour certaines données retournées. Ou peut-être avez-vous besoin de certaines commandes ou de certains événements
LocoNet ou CAN bus.

Vous êtes tout à fait libre de personnaliser une version locale de Z21Client afin de répondre à vos propres besoins.

## Vous avez trouvé un bug ?

Veuillez créer une issue dans le dépôt.

## Problèmes connus (actuellement)

Aucun.

## FAQ

### Z21Client fonctionnera-t-il avec la Z21 START newGen ?

À l'heure actuelle, je n'ai pas eu la possibilité de tester Z21Client
avec la Z21 START newGen et je ne peux donc pas dire si Z21Client
fonctionne avec la Z21 START newGen.

Le manuel "Z21 LAN Protocol" n'a pas non plus été mis à jour pour
inclure les modifications du protocole Z21 qui pourraient être
nécessaires pour la Z21 START newGen.

### Puis-je utiliser Z21Client avec ma Digikeijs DR5000 ?

Oui, c'est possible. Cependant, vous devez avoir configuré le protocole
réseau sur "Z21" dans les paramètres de la DR5000. Vous ne pouvez pas
utiliser Z21Client avec la DR5000 si vous avez sélectionné un autre
protocole.

Z21Client a été testé avec une DR5000 équipée du firmware 1.6.3 et
fonctionne correctement.

Veuillez noter que les centrales Z21 sont des appareils multiprotocoles,
alors que la DR5000 est exclusivement une centrale DCC. Comme le
firmware 1.6.3 de la DR5000, avec le protocole Z21, s'identifie comme
une Z21 (dans un boîtier noir) avec la version de firmware 1.29, il n'est
pas possible de lui demander quels protocoles sont activés. Vous verrez
donc à la fois les protocoles DCC et Märklin Motorola dans Z21Client.
La sélection du protocole Märklin Motorola pour les locomotives ou les
aiguillages n'aura aucun effet.

### Lorsque j’appelle la méthode `QueryForZ21s`, ma Z21 n’apparaît pas

La liste renvoyée par la méthode est vide et aucune erreur n’est signalée.

Vous pouvez vous connecter à votre centrale Z21 (tous les modèles) à l’aide de Z21Client, envoyer des commandes et
recevoir des données. Le fait que la méthode renvoie une liste vide se produit généralement lorsque le PC est connecté
au réseau sans fil.

Pour rechercher les appareils Z21 sur le réseau, `QueryForZ21s` envoie une diffusion UDP (« UDP broadcast ») à
laquelle les centrales Z21 doivent répondre.

De nombreux points d’accès et routeurs bloquent les diffusions UDP. Dans ce cas, votre Z21 peut ne pas recevoir la
diffusion et ne répond donc pas. Il est également possible que votre PC ne reçoive pas la réponse de la Z21.

Vérifiez la configuration de votre point d’accès ou de votre routeur afin de vous assurer que les diffusions UDP ne
sont pas bloquées.

Certains routeurs et points d’accès disposent également d’un paramètre permettant de bloquer les diffusions UDP
uniquement sur le réseau sans fil. D’autres appareils ne proposent pas ce paramètre, mais bloquent par défaut les
diffusions UDP sur le réseau Wi-Fi.

Dans ce cas, vous pouvez essayer de connecter votre PC au réseau à l’aide d’un câble Ethernet. Si cela résout le
problème, il est probable que votre point d’accès ou votre routeur bloque les diffusions UDP sur le réseau sans fil.

### Envisagez-vous d’implémenter les fonctionnalités LocoNet et CAN bus ?

La réponse courte est : non.

La réponse plus longue est que je ne possède pas de Z21 ni de Z21 XL. Je n’ai donc pas besoin de ces fonctionnalités
et je ne peux pas les tester.

### Envisagez-vous d’implémenter la prise en charge d’une connexion sans fil à la Z21 ?

En réalité, cela n’est pas nécessaire. Si votre réseau est correctement configuré et que vous utilisez un Roco 10814
ou votre propre point d’accès, vous pouvez accéder à la Z21 via le réseau sans fil.

Mon projet Z21Dashboard a été testé via WLAN et fonctionne correctement. Cependant, il m’est parfois arrivé de devoir
établir la connexion plus d’une fois.

### Comment commencer à développer ma propre application ?

Consultez Z21Client, et en particulier l’application Z21Dashboard, afin de voir comment elle est implémentée et de
vous en inspirer pour créer votre propre application.

## Liste des commandes du protocole LAN Z21 implémentées

Pour consulter la liste des commandes du protocole LAN Z21 implémentées dans Z21Client, reportez-vous au tableau
**« Commandes du protocole LAN Z21 implémentées »**.

<hr>

## Implemented Z21 LAN Protocol Commands

| Z21 Protocol Command (v1.13) | Implementation Status (Public Method) |
| :--- | :--- |
| **System, Status & Version** | |
| LAN_GET_SERIAL_NUMBER | GetSerialNumberAsync |
| LAN_LOGOFF | DisconnectAsync |
| LAN_X_GET_VERSION | [Not implemented] |
| LAN_X_GET_STATUS | GetStatusAsync |
| LAN_X_SET_TRACK_POWER_OFF | SetTrackPowerOffAsync |
| LAN_X_SET_TRACK_POWER_ON | SetTrackPowerOnAsync |
| LAN_X_SET_STOP | SetEmergencyStopAsync | 
| LAN_GET_FIRMWARE_VERSION | GetFirmwareVersionAsync |
| LAN_SET_BROADCASTFLAGS | SetBroadcastFlags (Private) |
| LAN_GET_BROADCASTFLAGS | GetBroadcastFlagsAsync |
| LAN_SYSTEMSTATE_GETDATA | GetSystemStateAsync |
| LAN_GET_HWINFO | GetHardwareInfoAsync |
| LAN_GET_CODE | GetZ21CodeAsync |
| **Settings** | |
| LAN_GET_LOCOMODE | GetLocoModeAsync |
| LAN_SET_LOCOMODE | SetLocoModeAsync |
| LAN_GET_TURNOUTMODE | GetTurnoutModeAsync |
| LAN_SET_TURNOUTMODE | SetTurnoutModeAsync |
| **Driving** | |
| LAN_X_GET_LOCO_INFO | GetLocoInfoAsync |
| LAN_X_SET_LOCO_DRIVE | SetLocoDriveAsync |
| LAN_X_SET_LOCO_FUNCTION | SetLocoFunctionAsync |
| LAN_X_SET_LOCO_FUNCTION_GROUP | [Not implemented] |
| LAN_X_SET_LOCO_BINARY_STATE | [Not implemented] |
| LAN_X_SET_LOCO_E_STOP | [Not implemented] |
| LAN_X_PURGE_LOCO | [Not implemented] |
| **Switching** | |
| LAN_X_GET_TURNOUTINFO | GetTurnoutInfoAsync |
| LAN_X_SET_TURNOUT | SetTurnoutPositionAsync |
| LAN_X_GET_TURNOUT_MODE | GetTurnoutModeAsync |
| LAN_X_SET_TURNOUT_MODE | SetTurnoutModeAsync |
| LAN_X_SET_EXT_ACCESSORY | [Not implemented] |
| LAN_X_GET_EXT_ACCESSORY_INFO | [Not implemented] |
| **Reading and writeing decoder CVs** | |
| LAN_X_CV_READ | GetCVValueFromProgTrackAsync |
| LAN_X_CV_WRITE | SetCVValueOnProgTrackAsync |
| LAN_X_CV_POM_WRITE_BYTE | SetCVValueOnPOMAsync |
| LAN_X_CV_POM_WRITE_BIT | SetCVBitOnPOMAsync |
| LAN_X_CV_POM_READ_BYTE | GetCVValueFromPOMAsync |
| LAN_X_CV_POM_ACCESSORY_WRITE_BYTE | [Not Implemented] |
| LAN_X_CV_POM_ACCESSORY_WRITE_BIT | [Not Implemented] |
| LAN_X_CV_POM_ACCESSORY_READ_BYTE | [Not Implemented] |
| LAN_X_MM_WRITE_BYTE | [Not Implemented] |
| LAN_X_DCC_READ_REGISTER | [Not Implemented] |
| LAN_X_DCC_WRITE_REGISTER | [Not Implemented] |
| **Feedback (R-Bus)** | |
| LAN_RMBUS_GETDATA | GetRBusDataAsync |
| LAN_RMBUS_PROGRAMMODULE | [Not Implemented] |
| **RailCom** | |
| LAN_RAILCOM_GETDATA | GetRailComDataAsync / GetNextRailComDataAsync |
| **LocoNet** | |
| LAN_LOCONET_FROM_LAN | [Not Implemented] |
| LAN_LOCONET_DISPATCH_ADDR | [Not Implemented] |
| LAN_LOCONET_DETECTOR | [Not Implemented] |
| **CAN** | |
| LAN_CAN_DETECTOR | [Not Implemented] |
| LAN_CAN_DEVICE_GET_DESCRIPTION | [Not Implemented] |
| LAN_CAN_DEVICE_SET_DESCRIPTION | [Not Implemented] |
| LAN_CAN_BOOSTER_SET_TRACKPOWER | [Not Implemented] |
| **Fast Clock** | |
| LAN_FAST_CLOCK_CONTROL | [Not implemented] |
| LAN_FAST_CLOCK_DATA | [Not implemented] |
| LAN_FAST_CLOCK_SETTINGS_GET | [Not implemented] |
| LAN_FAST_CLOCK_SETTINGS_SET | [Not implemented] |

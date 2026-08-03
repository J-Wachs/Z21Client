[For English version, click here](#z21client-c-class)<br />
[Für die deutsche Version hier klicken](#z21client-c-klasse-deutsch)

Danish version:
# Z21Client C# klasse

En C#-klasse til kommunikation med z21, z21Start, Z21 og Z21 XL centralstationerne til modeltogsbaner fra Roco/Fleischmann.

Z21Client-klassen understøtter følgende funktioner:

* Sprogversionering, dansk/tysk når sprog i Windows er sat til dansk/tysk. For alle andre sprog vises tekster på engelsk
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

* Tilføjet meddelelser på tysk. Bemærk venligst, at de tyske meddelelser er oversat af en AI, og derfor kan være
  mindre korrekte end de danske og engelske meddelelser.
* Tilføjet mange nye tests til testprojektet

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

### Når jeg kalder metoden QueryForZ21s vises min Z21 ikke

Listen som metoden returnerer er tom, og du får ingen fejl. Du kan forbinde med Z21Client til din Z21 centralstation (alle
modeller), og sende kommandoer og modtage data. At metoden returnerer en tom liste, sker typisk når pc'en er koblet på
netværket trådløst.

For at finde Z21'ere på netværket udsender QueryForZ21s en UDP-broadcast som Z21 centralstationerne skal svare på. Mange
access points og routere blokerer for UDP-broadcasts, og det er derfor muligt, at din Z21 ikke modtager broadcastet og
derfor ikke svarer på det. Det er også muligt, at din pc ikke modtager svaret fra Z21.

Kik i opsætningen af dit access point eller router og se, om der er en indstilling for at blokere for UDP-broadcasts.
Hvis det er tilfældet, skal du slå denne indstilling fra. Visse routere og access points har også en indstilling for
at blokere for UDP-broadcasts på det trådløse net alene. Andre access points og routere har ikke en indstilling, men blokerer
for UDP-broadcasts på det trådløse net som standard. I dette tilfælde kan du prøve at forbinde din pc til netværket med kabel
for at se, om det løser problemet. Hvis det gør det, er det sandsynligt, at dit access point eller router blokerer for
UDP-broadcasts på det trådløse net.

### Vil du implementere LocoNet- og CAN-bus-funktionalitet?

Det korte svar er nej. Det lange svar er, at jeg ikke ejer en Z21 eller Z21 XL, derfor har jeg ikke behovet og kan
ikke teste funktionaliteten.

### Vil du implementere understøttelse af trådløs forbindelse til Z21?

Faktisk – hvis dit netværk er konfigureret korrekt, og du har Roco 10814 eller bruger dit eget access-point, kan du
få trådløs adgang til Z21. Mit projekt Z21Dashboard er testet over trådløst LAN, og det virker fint. Nogle gange
skulle jeg dog oprette forbindelse mere end én gang.

### Hvordan kommer jeg i gang med at skrive min egen applikation?

Tag et kig på Z21Client – særligt Z21Dashboard-applikationen – for at se, hvordan den er implementeret og
for inspiration til, hvad du selv kan lave.

## Liste over implementerede Z21 LAN Protokol-kommandoer
For at se en oversigt over implementerede Z21 LAN Protokol-kommandoer, se tabellen ved at klikke [her](#implemented-z21-lan-protocol-commands).

<hr>

# Z21Client C# class

A C# class to communicate with the z21, z21Start, Z21 and Z21 XL model railroad central station from Roco/Fleischmann.

The Z21Client class supports the following features:

* Multi language. Danish/German when language is set to Danish/German in Windows. For all other languages texts are in English
* Connect to the Z21 via UDP
* Received information about locomotives (speed, direction, functions, protocol) when other driving controls are used
* Received information about turnouts/points (position, protocol) when other controls are used
* Send commands to control locomotives (speed, direction, functions, protocol)
* Send commands to control turnouts/points (position, protocol)
* Read feedback from the Z21 (e.g., locomotive status)
* Support for multiple locomotives
* Event-driven architecture for handling responses and updates
* Asynchronous operations for non-blocking communication
* Error handling and reconnection logic
* Support for the protocols used by the z21/Z21 (DCC, Märklin Motorola)
* Logging capabilities for debugging and monitoring
* Z21Client has been developed using the 'AI Pair Programming' method

## What is new in this version

* Added messages in German. Please note that the German messages are translated by an AI, and therefore may
  be less accurate than the Danish and English messages
* Added many tests to the test project

## z21 and z21Start locking information

In case of your z21 or z21Start is locked, you can still send the commands to it with this class. However, 
the commands will be ignored by the z21/z21Start.

If the z21/z21Start is locked, you can still use the Z21Client class, to write a monitoring application that reads 
status of locomotives and turnouts/points. You can also call methods in Z21Client to change the protocol of
locomotives and turnouts/points decoders. You can read more about which methods (that wraps the Z21 commands) you can
call when the z21/z21Start is locked, in the official Z21 LAN Protocol documentation on the Z21 website.

Please note, that when the z21 (in white case) initially was launched, some was locked, and some was unlocked. To
unlock your z21 or z21Start, you can purchase an unlock code:

* Roco item 10814. Contains a wireless access point and unlock code for z21Start and z21 (white case)
* Roco item 10818. Contains unlock code for z21Start and z21 (white case)

From here on, the term Z21 will be used to refer to all four versions of the Z21 family of central stations. If
something applies to only one of the versions, it will be specified.

The Z21Client was developed and tested using two z21Start central stations; one locked and one unlocked. This is
the reason why none of the LocoNet and CAN bus functionality is implemented in the Z21Client class.

The implementation is based on the Roco document *'Z21 LAN Protocol Specification'*, version 1.13 EN, dated 6
November 2023. The document can be downloaded from the Z21 website.

## Fully functional example project

To see an example of how to use the Z21Client, please visit my project 'Z21Dashboard' on Github:

https://github.com/J-Wachs/Z21Dashboard

## How does it work?

The Z21Client class uses UDP to communicate with the Z21 central station. In your application, you must first
establish a connection to the Z21.

As the architecture of the Z21Client class is event-driven, you must subscribe to the events you want to handle
in your application. For example, to handle locomotive status updates, you would subscribe to the
*'LocoInfoReceived'* event.

The necessary changes to the broadcast flags on the Z21 are handled automatically by the Z21Client class when you
add your method to the Z21Client event.

Example of subscribing to the LocoInfoReceived event:
```csharp
...
using IZ21Client Z21Client
...

...
Z21Client.OnLocoInfoReceived += OnLocoInfoReceived;
...

private async void OnLocoInfoReceived(object? sender, LocoInfo e)
{
	// Handle the locomotive info received event
	Console.WriteLine($"Loco Info Received: Address={e.Address}, Speed={e.CurrentSpeed}, Direction={e.Direction}");
}
```

### Implementation of Märklin Motorola protocol in Z21Client vs in Z21

The Z21 supports both DCC and Märklin Motorola protocols for controlling locomotives. The following versions of the 
protocols are implemented as follows:
* DCC, 14 speed steps: Protocol = DCC, speed steps = 14
* DCC, 28 speed steps: Protocol = DCC, speed steps = 28
* DCC, 128 speed steps: Protocol = DCC, speed steps = 128
* Märklin Motorola 1, 14 speed steps: Protocol = Märklin Motorola, speed steps = 14
* Märklin Motorola 2, 14 speed steps: Protocol = Märklin Motorola, speed steps = 28
* Märklin Motorola 2, 28 speed steps: Protocol = Märklin Motorola, speed steps = 128

Because of this, the Z21 reports and expects the speed steps to be in the range 14, 28 or 128, even when using the
Märklin Motorola protocol. The Z21Client has been developed to reflect the protocol and speed steps as one would
expect it to be. Thus, when using the Märklin Motorola protocol, the speed steps will be 14, 14 or 28 respectively.

The locomotive information class 'LocoInfo' used in the Z21Client class, reflects this implementation, and have two 
speed step properties:
* SpeedSteps: The speed steps as implemented in the Z21Client class (DCC: 14, 28 and 128; MM: 14, 14, 28 speed steps)
* NativeSpeedSteps: The speed steps as implemented in the Z21. Will be 14, 28 or 128 also for the MM protocol.

### Disclaimer: Implementation of not documented 'Locomotive Slot Information'

Roco have in their tool *Maintenance Tool* an option to see the 120 locomotive slots that are in the Z21. However,
the official *'Z21 LAN Protocol Specification'* documentation does not mention the command and response to read these slots.
By monitoring the data sent between my Z21s (two z21Start, one locked, one unlocked) I could see the commands.
Because of this, I have implemented undocumented functionality. It works in firmware 1.43
(part of Maintenance Tool V1.18.3). There is no guarantee that this command and the response will work in future
releases of the firmware.

## Workaround for Z21 firmware bug

In the latest firmware version (1.43) for the Z21 family, there is what seems like a bug to me. When explicitly
requesting information about a locomotive (that is, you call the Z21 command LAN_X_GET_LOCO_INFO, wrapped in
Z21Client.GetLocoInfoAsync()), the protocol bit in byte DB2 in the response is not set for locomotives configured
to use the Märklin Motorola protocol. However, the protocol bit is correctly set in events caused by changes to the
locomotive (for example speed, direction, function keys).

To work around this bug, Z21Client requests the protocol of the locomotive separately and then provides the correct
protocol in the LocoInfoReceived event.

## Installation instructions

### Getting and trying out the Z21Client class

Download the repo and create a project in which to use the Z21Client. If you need inspiration, please see my project
'Z21Dashboard' on Github:

https://github.com/J-Wachs/Z21Dashboard

### Setting up your own project to use the Z21Client class

To use the Z21Client class in your own projects, you must add the component project to your solution. Then you must
add the 'Z21Client' to the Program.cs, or MauiProgram.cs, file of your project:

```csharp
...
// Added for Z21Client
builder.Services.AddSingleton<IZ21UdpClient, Z21UdpClient>();
builder.Services.AddSingleton<IZ21Client, Z21Client>();
// End
...
```

## Modifying the Z21Client for your own use

Maybe you need more information to be returned. Maybe you need to use a config value for some of the data returned.
Maybe you need one of the LocoNet or CAN bus commands/events.

Please feel free to adapt a local version to fit your needs.

## Found a bug?

Please create an issue in the repo.

## Known issues (Work in progress)

None at this time.

## FAQ

### When I call the QueryForZ21s method, my Z21 is not shown

The list returned by the method is empty, and no error is raised. You can still
connect with Z21Client to your Z21 central station (all models), send commands,
and receive data. The method typically returns an empty list when the PC is
connected to the network wirelessly.

To discover Z21 devices on the network, QueryForZ21s sends a UDP broadcast that
the Z21 central stations must respond to. Many access points and routers block
UDP broadcasts, which may prevent your Z21 from receiving the broadcast and
responding. It is also possible that your PC does not receive the response
from the Z21.

Check the configuration of your access point or router to see if there is a
setting that blocks UDP broadcasts. If so, disable this setting. Some routers
and access points also have a setting to block UDP broadcasts on the wireless
network only. Other routers and access points do not expose such a setting but
block UDP broadcasts on the wireless network by default.

In that case, try connecting your PC to the network using a cable to see if
that resolves the problem. If it does, it is likely that your access point or
router blocks UDP broadcasts on the wireless network.

### Will you implement LocoNet and CAN bus functionality?

The short answer is no. The long answer is that I do not own a Z21 or Z21 XL hense I do not have the need, and I 
cannot test the functionality.

### Will you implement support for wireless connection to the Z21?

Actually, if your network is setup correctly, and you have the Roco 10814 or use your own access point, you can
access the Z21 wirelessly. My Z21Dashbord is tested over a wireless LAN, and it works fine. Some times I needed
to connect more than once.

### How do I get started, writing my own application?

Take a look at the Z21Client especially the Z21Dashboard application, in order to see how it is implemented and 
get inspired on what you can do with it.

## List of implemented Z21 LAN Protocol commands
To see a list of the implemented Z21 LAN Protocol commands, see the table by clicking
[here](#implemented-z21-lan-protocol-commands).

<hr>

# Z21Client C# Klasse, Deutsch

Eine C#-Klasse zur Kommunikation mit den z21-, z21Start-, Z21- und
Z21 XL-Modellbahn-Zentralstationen von Roco/Fleischmann.

Die Z21Client-Klasse unterstützt folgende Funktionen:

* Mehrsprachig. Dänisch/Deutsch, wenn in Windows Dänisch/Deutsch eingestellt ist.
  Für alle anderen Sprachen werden die Texte auf Englisch angezeigt.
* Verbindung zur Z21 über UDP
* Empfangen von Informationen über Lokomotiven (Geschwindigkeit, Richtung,
  Funktionen, Protokoll), wenn andere Steuerungen verwendet werden
* Empfangen von Informationen über Weichen (Position, Protokoll), wenn
  andere Steuerungen verwendet werden
* Senden von Kommandos zur Steuerung von Lokomotiven (Geschwindigkeit,
  Richtung, Funktionen, Protokoll)
* Senden von Kommandos zur Steuerung von Weichen (Position, Protokoll)
* Lesen von Rückmeldungen der Z21 (z. B. Lokstatus)
* Unterstützung mehrerer Lokomotiven
* Ereignisgesteuerte Architektur zur Verarbeitung von Antworten und
  Aktualisierungen
* Asynchrone Operationen für nicht blockierende Kommunikation
* Fehlerbehandlung und Wiederverbindungslogik
* Unterstützung der von z21/Z21 verwendeten Protokolle (DCC, Märklin
  Motorola)
* Logging-Möglichkeiten zur Fehlerbehebung und Überwachung
* Z21Client wurde mit der Methode "AI Pair Programming" entwickelt

## Was ist neu in dieser Version

* Meldungen auf Deutsch hinzugefügt. Bitte beachten Sie, dass die deutschen Meldungen von einer KI
  übersetzt wurden und daher möglicherweise weniger genau sind als die dänischen und englischen Meldungen
* Viele Tests zum Testprojekt hinzugefügt

## z21- und z21Start-Sperrinformation

Wenn Ihre z21 oder z21Start gesperrt ist, können Sie weiterhin Kommandos an
sie senden. Diese werden jedoch von der z21/z21Start ignoriert.

Wenn die z21/z21Start gesperrt ist, können Sie die Z21Client-Klasse dennoch
verwenden, um eine Überwachungsanwendung zu schreiben, die den Status von
Lokomotiven und Weichen ausliest. Sie können auch Methoden in Z21Client
aufrufen, um das Protokoll von Lokomotiven und Weichendekodern zu ändern.
Mehr über die Methoden (die z21-Kommandos kapseln), die bei gesperrter
z21/z21Start aufgerufen werden können, erfahren Sie in der offiziellen Z21
LAN Protocol-Dokumentation auf der Z21-Website.

Bitte beachten Sie, dass bei der Markteinführung der z21 (im weißen
Gehäuse) einige Geräte gesperrt und andere freigeschaltet waren. Um Ihre
z21 oder z21Start zu entsperren, können Sie einen Freischaltcode erwerben:

* Roco Artikel 10814. Enthält einen WLAN-Access-Point sowie einen
  Freischaltcode für z21Start und z21 (weißes Gehäuse)
* Roco Artikel 10818. Enthält einen Freischaltcode für z21Start und z21
  (weißes Gehäuse)

Im Folgenden wird der Begriff Z21 für alle vier Versionen der Z21-Familie
von Zentralstationen verwendet. Wenn etwas nur für eine der Versionen gilt,
wird dies angegeben.

Der Z21Client wurde mit zwei z21Start-Zentralstationen entwickelt und
getestet, einer gesperrten und einer freigeschalteten. Das ist der Grund,
warum keine LocoNet- und CAN-Bus-Funktionalität in der Z21Client-Klasse
implementiert ist.

Die Implementierung basiert auf dem Roco-Dokument *"Z21 LAN Protocol
Specification"*, Version 1.13 EN, vom 6. November 2023. Das Dokument kann
von der Z21-Website heruntergeladen werden.

## Voll funktionsfähiges Beispielprojekt

Um ein Beispiel für die Nutzung des Z21Client zu sehen, besuchen Sie bitte
mein Projekt *Z21Dashboard* auf Github:

[https://github.com/J-Wachs/Z21Dashboard](https://github.com/J-Wachs/Z21Dashboard)

## Wie funktioniert es?

Die Z21Client-Klasse verwendet UDP, um mit der Z21-Zentralstation zu
kommunizieren. In Ihrer Anwendung müssen Sie zunächst eine Verbindung zur
Z21 herstellen.

Da die Architektur der Z21Client-Klasse ereignisgesteuert ist, müssen Sie
auf die Ereignisse abonnieren, die Sie in Ihrer Anwendung verarbeiten
möchten. Um beispielsweise Aktualisierungen des Lokstatus zu verarbeiten,
abonnieren Sie das Ereignis *'OnLocoInfoReceived'*.

Die notwendigen Änderungen an den Broadcast-Flags der Z21 werden
automatisch von der Z21Client-Klasse vorgenommen, wenn Sie Ihre Methode
dem Z21Client-Ereignis hinzufügen.

Beispiel für die Anmeldung am OnLocoInfoReceived-Ereignis:
```csharp
...
using IZ21Client Z21Client
...

...
Z21Client.OnLocoInfoReceived += OnLocoInfoReceived;
...

private async void OnLocoInfoReceived(object? sender, LocoInfo e)
{
	// Empfangene Lokinformation verarbeiten
	Console.WriteLine($"Loco Info Received: Address={e.Address},
	Speed={e.CurrentSpeed}, Direction={e.Direction}");
}
```

### Implementierung des Märklin Motorola-Protokolls in Z21Client vs. Z21

Die Z21 unterstützt sowohl DCC als auch Märklin Motorola zur Steuerung von
Lokomotiven. Die folgenden Protokollversionen sind wie folgt implementiert:
* DCC, 14 Fahrstufen: Protokoll = DCC, Fahrstufen = 14
* DCC, 28 Fahrstufen: Protokoll = DCC, Fahrstufen = 28
* DCC, 128 Fahrstufen: Protokoll = DCC, Fahrstufen = 128
* Märklin Motorola 1, 14 Fahrstufen: Protokoll = Märklin Motorola,
  Fahrstufen = 14
* Märklin Motorola 2, 14 Fahrstufen: Protokoll = Märklin Motorola,
  Fahrstufen = 28
* Märklin Motorola 2, 28 Fahrstufen: Protokoll = Märklin Motorola,
  Fahrstufen = 128

Deshalb meldet und erwartet die Z21 die Fahrstufen im Bereich 14, 28 oder
128, auch wenn das Märklin Motorola-Protokoll verwendet wird. Z21Client
wurde entwickelt, um das Protokoll und die Fahrstufen so wiederzugeben, wie
man es normalerweise erwarten würde. Beim Märklin Motorola-Protokoll sind
die Fahrstufen also 14, 14 bzw. 28.

Die *LocoInfo*-Klasse in Z21Client spiegelt diese Implementierung wider und
hat zwei Fahrstufen-Eigenschaften:
* **SpeedSteps:** Die Fahrstufen wie in Z21Client implementiert (DCC: 14,
  28, 128; MM: 14, 14, 28)
* **NativeSpeedSteps:** Die Fahrstufen wie in der Z21 implementiert. Immer
  14, 28 oder 128 - auch für das MM-Protokoll.

### Hinweis: Implementierung der nicht dokumentierten "Locomotive Slot Information"

Roco bietet in seinem Werkzeug *Maintenance Tool* eine Möglichkeit, die 120
Lokomotiv-Slots in der Z21 anzuzeigen. Die offizielle *"Z21 LAN Protocol
Specification"*-Dokumentation erwähnt jedoch nicht das Kommando und die
Antwort zum Auslesen dieser Slots. Durch die Überwachung der Daten zwischen
meinen Z21s (zwei z21Start, eine gesperrt, eine freigeschaltet) konnte ich
die Kommandos erkennen.

Deshalb habe ich nicht dokumentierte Funktionalität implementiert. Sie
funktioniert in Firmware 1.43 (Bestandteil von Maintenance Tool V1.18.3).
Es gibt keine Garantie, dass dieses Kommando und die Antwort in zukünftigen
Firmware-Versionen funktionieren.

## Workaround für einen Z21-Firmware-Fehler

In der neuesten Firmware-Version (1.43) für die Z21-Familie gibt es meiner
Meinung nach einen Fehler. Wenn man explizit Informationen über eine
Lokomotive anfordert (d. h. das Z21-Kommando LAN_X_GET_LOCO_INFO aufruft,
verpackt in Z21Client.GetLocoInfoAsync()), wird das Protokoll-Bit in Byte
DB2 der Antwort für Lokomotiven, die für Märklin Motorola konfiguriert
sind, nicht gesetzt. Das Protokoll-Bit wird jedoch korrekt in Ereignissen
gesetzt, die durch Änderungen an der Lokomotive verursacht werden
(z. B. Geschwindigkeit, Richtung, Funktionstasten).

Um diesen Fehler zu umgehen, fordert Z21Client das Protokoll der
Lokomotive separat an und liefert dann das korrekte Protokoll im
OnLocoInfoReceived-Ereignis.

## Installationsanleitung

### Herunterladen und Ausprobieren der Z21Client-Klasse

Laden Sie das Repo herunter und erstellen Sie ein Projekt, in dem Sie
Z21Client verwenden möchten. Wenn Sie Inspiration brauchen, sehen Sie sich
bitte mein Projekt *Z21Dashboard* auf Github an:

[https://github.com/J-Wachs/Z21Dashboard](https://github.com/J-Wachs/Z21Dashboard)

### Einrichten des eigenen Projekts für die Nutzung der Z21Client-Klasse

Um die Z21Client-Klasse in eigenen Projekten zu verwenden, müssen Sie das
Komponentenprojekt zu Ihrer Lösung hinzufügen. Anschließend müssen Sie
*Z21Client* in der Program.cs- oder MauiProgram.cs-Datei Ihres Projekts
hinzufügen:

```csharp
...
// Hinzugefügt für Z21Client
builder.Services.AddSingleton<IZ21UdpClient, Z21UdpClient>();
builder.Services.AddSingleton<IZ21Client, Z21Client>();
// Ende
...
```

## Anpassen des Z21Client für den eigenen Gebrauch

Vielleicht benötigen Sie mehr zurückgegebene Informationen. Vielleicht
möchten Sie einen Konfigurationswert für einige der zurückgegebenen Daten
verwenden. Vielleicht benötigen Sie eines der LocoNet- oder
CAN-Bus-Kommandos/ Ereignisse.

Bitte passen Sie eine lokale Version gerne an Ihre Bedürfnisse an.

## Einen Fehler gefunden?

Bitte erstellen Sie ein Issue im Repo.

## Bekannte Probleme (in Arbeit)

Derzeit keine.

## FAQ

### Wenn ich die Methode QueryForZ21s aufrufe, wird meine Z21 nicht angezeigt

Die zurückgegebene Liste ist leer, und es wird kein Fehler ausgelöst. Sie
können sich weiterhin mit Z21Client mit Ihrer Z21-Zentralstation (alle
Modelle) verbinden, Kommandos senden und Daten empfangen. Die Methode gibt
typischerweise eine leere Liste zurück, wenn der PC drahtlos mit dem
Netzwerk verbunden ist.

Um Z21-Geräte im Netzwerk zu finden, sendet QueryForZ21s einen
UDP-Broadcast, auf den die Z21-Zentralstationen antworten müssen. Viele
Access Points und Router blockieren UDP-Broadcasts, wodurch Ihre Z21 den
Broadcast möglicherweise nicht empfängt und nicht antwortet. Es ist auch
möglich, dass Ihr PC die Antwort von der Z21 nicht empfängt.

Prüfen Sie die Konfiguration Ihres Access Points oder Routers, ob es eine
Einstellung gibt, die UDP-Broadcasts blockiert. Falls ja, deaktivieren Sie
diese Einstellung. Einige Router und Access Points haben auch eine
Einstellung, die UDP-Broadcasts nur im drahtlosen Netzwerk blockiert.
Andere blockieren UDP-Broadcasts im drahtlosen Netzwerk standardmäßig ohne
entsprechende Einstellung. Verbinden Sie in diesem Fall Ihren PC testweise
per Kabel mit dem Netzwerk, um zu prüfen, ob das Problem behoben wird.
Wenn ja, blockiert Ihr Access Point oder Router wahrscheinlich
UDP-Broadcasts im drahtlosen Netzwerk.

### Werden Sie LocoNet- und CAN-Bus-Funktionalität implementieren?

Die kurze Antwort ist nein. Die lange Antwort ist, dass ich keine Z21 oder
Z21 XL besitze, daher besteht für mich kein Bedarf und ich kann die
Funktionalität nicht testen.

### Werden Sie Unterstützung für eine drahtlose Verbindung zur Z21 implementieren?

Tatsächlich können Sie, wenn Ihr Netzwerk korrekt konfiguriert ist und Sie
Roco 10814 besitzen oder Ihren eigenen Access Point verwenden, drahtlos
auf die Z21 zugreifen. Mein Projekt Z21Dashboard wurde über ein drahtloses
LAN getestet und funktioniert einwandfrei. Manchmal musste ich mich jedoch
mehr als einmal verbinden.

### Wie fange ich an, meine eigene Anwendung zu schreiben?

Sehen Sie sich Z21Client an - insbesondere die Z21Dashboard-Anwendung -
um zu sehen, wie sie implementiert ist, und lassen Sie sich inspirieren,
was Sie selbst damit machen können.

## Liste der implementierten Z21 LAN Protocol-Kommandos

Für eine Übersicht über die implementierten Z21 LAN Protocol-Kommandos
siehe die Tabelle, indem Sie
[hier klicken](#implemented-z21-lan-protocol-commands).

<hr>

## Implemented Z21 LAN Protocol Commands

| Z21 Protocol Command (v1.13) | Implementation Status (Public Method) |
| :--- | :--- |
| **System, Status & Version** | |
| LAN_GET_SERIAL_NUMBER | GetSerialNumberAsync |
| LAN_LOGOFF | DisconnectAsync |
| LAN_X_GET_VERSION | [Not implemented] |
| LAN_X_GET_STATUS | GetSystemStateAsync |
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

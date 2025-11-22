[Click here for English version](#z21-client-c#-client)

Danish version:
# Z21Client C# klasse

En C#-klasse til kommunikation med z21, z21Start, Z21 og Z21 XL modelbanecentralerne fra Roco/Fleischmann.

Z21Client-klassen understøtter følgende funktioner:

* Forbindelse til Z21 via TCP/IP
* Modtage information om lokomotiver (hastighed, retning, funktioner, protokol), når andre styreenheder bruges
* Modtage information om sporskifter/points (position, protokol), når andre styreenheder bruges
* Sende kommandoer til styring af lokomotiver (hastighed, retning, funktioner, protokol)
* Sende kommandoer til styring af sporskifter/points (position, protokol)
* Læse feedback fra Z21 (f.eks. lokomotivstatus)
* Understøttelse af flere lokomotiver
* Event-drevet arkitektur til håndtering af svar og opdateringer
* Asynkrone operationer for ikke-blokerende kommunikation
* Fejlhåndtering og genforbindelseslogik
* Understøttelse af forskellige protokoller brugt af z21/Z21 (DCC, Märklin Motorola)
* Logging-muligheder til fejlfinding og overvågning

## z21 og z21Start låseinformation

Hvis din z21 eller z21Start er låst, kan du stadig sende kommandoer til den med denne klasse. Dog vil
kommandoerne blive ignoreret af z21/z21Start.

Hvis z21/z21Start er låst, kan du stadig bruge Z21Client-klassen til at skrive et overvågningsprogram, der læser
status for lokomotiver og sporskifter/points.

Bemærk, at da z21 (i hvidt kabinet) oprindeligt blev lanceret, var nogle låste og andre ulåste. For at låse
din z21 eller z21Start op, kan du købe en oplåsningskode:

* Roco vare 10818. Indeholder oplåsningskode til z21Start og z21 (hvidt kabinet).
* Roco vare 10814. Indeholder et trådløst access-point samt oplåsningskode til z21Start og z21 (hvidt kabinet).

Fra nu af vil betegnelsen Z21 blive brugt om alle fire versioner af Z21-familien af centralstationer. Hvis noget
kun gælder én af versionerne, vil det blive angivet.

Z21Client blev udviklet og testet ved brug af to z21Start: én låst og én ulåst. Dette er grunden til, at hverken
LocoNet- eller CAN-bus-funktionalitet er implementeret i Z21Client-klassen.

Implementeringen er baseret på Roco-dokumentet *"Z21 LAN protocol Specification"*, version 1.13 EN, dateret 6.
november 2023. Dokumentet kan downloades fra Z21-websitet.

## Fuldt funktionelt eksempelprojekt

For at se et eksempel på brugen af Z21Client, besøg venligst mit projekt *Z21Dashboard* på Github:

[https://github.com/J-Wachs/Z21Dashboard](https://github.com/J-Wachs/Z21Dashboard)

## Hvordan virker det?

Z21Client-klassen bruger UDP til at kommunikere med Z21-centralen. I din applikation skal du først
oprette forbindelse til Z21.

Da arkitekturen i Z21Client-klassen er event-drevet, skal du abonnere på de events, du ønsker at håndtere
i din applikation. For eksempel skal du abonnere på eventet *‘LocoStatusReceived’* for at håndtere
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
* **NativeSpeedSteps:** Hastighedstrin som implementeret i Z21. Altid 14, 28 eller 128 — også for MM-protokollen.

### Disclaimer: Implementering af ikke-dokumenteret 'Locomotive Slot Information'

Roco har i deres værktøj *Maintenance Tool* en mulighed for at se de 120 lokomotiv-slots, der findes i Z21. Men
den officielle *Z21 LAN Protocol* dokumentation nævner ikke kommandoen og svaret til at læse disse slots.
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

Download repo’et og opret et projekt, hvor du vil bruge Z21Client. Hvis du mangler inspiration, kan du se mit projekt
*Z21Dashboard* på Github:

[https://github.com/J-Wachs/Z21Dashboard](https://github.com/J-Wachs/Z21Dashboard)

### Opsætning af dit eget projekt til at bruge Z21Client-klassen

For at bruge Z21Client-klassen i dine egne projekter skal du tilføje komponentprojektet til din løsning. Derefter skal
du tilføje *Z21Client* til Program.cs eller MauiProgram.cs i dit projekt:

```csharp
...
// Tilføjet for Z21Client
builder.Services.AddSingleton<IZ21Cleint, Z21Client>();
// Slut
...
```

## Tilpasning af Z21Client til eget brug

Måske har du brug for flere oplysninger. Måske skal du bruge en konfigurationsværdi til nogle af de data, der returneres.
Måske har du brug for én af LocoNet- eller CAN-bus-kommandoerne/events.

Du er meget velkommen til at tilpasse en lokal version til dine behov.

## Fundet en fejl?

Opret venligst et issue i repo’et.

## Kendte problemer (pågår)

Ingen på nuværende tidspunkt.

## FAQ

### Vil du implementere LocoNet- og CAN-bus-funktionalitet?

Det korte svar er nej. Det lange svar er, at jeg ikke ejer en Z21 eller Z21 XL, derfor har jeg ikke behovet og kan
ikke teste funktionaliteten.

### Vil du implementere understøttelse af trådløs forbindelse til Z21?

Faktisk — hvis dit netværk er konfigureret korrekt, og du har Roco 10814 eller bruger dit eget access-point, kan du
få trådløs adgang til Z21. Mit projekt Z21Dashboard er testet over trådløst LAN, og det virker fint. Nogle gange
skulle jeg dog oprette forbindelse mere end én gang.

### Hvordan kommer jeg i gang med at skrive min egen applikation?

Tag et kig på Z21Client — særligt Z21Dashboard-applikationen — for at se, hvordan den er implementeret og
for inspiration til, hvad du selv kan lave.
<hr>

# Z21Client C# class

A C# class to communicate with the z21, z21Start, Z21 and Z21 XL model railroad central station from Roco/Fleischmann.

The Z21Client class supports the following features:
* Connect to the Z21 via TCP/IP
* Received information about locomotives (speed, direction, functions, protocol) when other driving controls are used
* Received information about turnouts/points (position, protocol) when other controls are used
* Send commands to control locomotives (speed, direction, functions, protocol)
* Send commands to control turnouts/points (position, protocol)
* Read feedback from the Z21 (e.g., locomotive status)
* Support for multiple locomotives
* Event-driven architecture for handling responses and updates
* Asynchronous operations for non-blocking communication
* Error handling and reconnection logic
* Support for various protocols used by the z21/Z21 (DCC, Märklin Motorola)
* Logging capabilities for debugging and monitoring

## z21 and z21Start locking information

In case of your z21 or z21Start is locked, you can still send the commands to it with this class. However, 
the commands will be ignored by the z21/z21Start.

If the z21/z21Start is locked, you can still use the Z21Client class, to write a monitoring application that reads 
status of locomotives and turnouts/points.

Please note, that when the z21 (in white case) initially was launched, some was locked, and some was unlocked. To
unlock your z21 or z21Start, you can purchase a unlock code:
* Roco item 10818. Contains unlock code for z21Start and z21 (white case).
* Roco item 10814. Contains a wireless access point and unlock code for z21Start and z21 (white case).

Form here on, the term Z21 will be used to refer to all four versions of the Z21 family of central stations. If
somthing apply to only one of the versions, it will be specified.

The Z21Client was developed and tested using two z21Start; one locked and one unlocked. This is the reason why none
of the LocoNet and CAN bus functionality is implemented in the Z21Client class.

The is implemented according to the Roco document 'Z21 LAN protocol Specification', version 1.13 EN, dated 6th of 
November 2023. The document can be downloaded from the Z21 website.

## Fully functional example project

To see an example of how to use the Z21Client, please visit my project 'Z21Dashboard' on Github:

https://github.com/J-Wachs/Z21Dashboard

## How does it work?

The Z21Client class uses UDP to communicate with the Z21 central station. In your application, you must first
establish a connection to the Z21.

As the archiceture of the Z21Client class is event-driven, you must subscribe to the events you want to handle
in your application. For example, to handle locomotive status updates, you would subscribe to the
'LocoStatusReceived' event.

The nessecary changes to the broadcast flags on the Z21 will be done automatically by the Z21Client class when you
add you method to the Z21Class event.

Example of subscribing to the LocoStatusReceived event:
```csharp
...
@using IZ21Client Z21Client
...

...
Z21Client.LocoInfoReceived += OnLocoInfoReceived;
...

private async void OnLocoInfoReceived(object? sender, LocoInfo e)
{
	// Handle the locomotive info received event
	Console.WriteLine($"Loco Info Received: Address={e.Address}, Speed={e.CurrentSpeed}, Direction={e.Direction}");"

```

### Implementation of Märklin Motorola protocol in Z21Client vs in Z21Client

The Z21 supports both DCC and Märklin Motorola protocols for controlling locomotives. The following versions of the 
protocols are implementen as follows:
* DCC, 14 speed steps: Protocol is DCC and speed steps is set to 14
* DCC, 28 speed steps: Protocol is DCC and speed steps is set to 28
* DCC, 128 speedsteps: Protocol is DCC and speed steps is set to 128
* Märklin Motorola 1, 14 speed steps: Protocol is Märklin Motorola and speed steps is set to 14
* Märklin Motorola 2, 14 speed steps: Protocol is Märklin Motorola and speed steps is set to 28
* Märklin Motorola 2, 28 speed steps: Protocol is Märklin Motorola and speed steps is set to 128

Because of this, the Z21 reports and expects the speed steps to be in the range 14, 28 or 128, even when using the
Märklin Motorola protocol. The Z21Client has been developed to reflect the protocol and speed steps as one would
expect it to be. Thus, when using the Märklin Motorola protocol, the speed steps will be 14, 14 or 28 respectively.

The locomotive information class 'LocoInfo' used in the Z21Client class, reflects this implementation, and have two 
speed step properties:
* SpeedSteps: The speed steps as implemented in the Z21Client class (DCC: 14, 28 and 128; MM: 14, 14, 28 speed steps)
* NativeSpeedSteps: The speed steps as implemented in the Z21. Will be 14, 28 or 128 also for the MM protocol.

### Disclaimer: Implementation of not documented 'Locomotive Slot Information'

Roco have in their tool 'Maintenance Tool' an option to see the 120 locomotive slots that is in the Z21. However,
the official 'Z21 LAN Protocol' documentation does not mention the command and reponse to read these slots.
By monitoring the data send between my Z21 (two z21Start, one locked, one unlocked) I could see the commands.
Because of this, I have implemented undocumented functionality. It works in firmware 1.43 (part of Maintenance Tool
V1.18.3). There is no guarantee that this command and the repsonse will work in future releases of the firmware.

## Workaround for Z21 firmware bug

In the latest firmware version (1.43) for the Z21 family, there is what seems like a bug to me. When explicitly
requesting information about a locomotive (that is, you call the Z21 command LAN_X_GET_LOCO_INFO, wrapped in
Z21Clint.GetLocoInfoAsync()). For locomotives configured to use the Märklin Motorola protocol the protocol-bit in
byte DB2 in the response, is not set. However, when you received events caused by changes to the locomotive, e.g.
speed, direction, F-keys the protocol-bit is set correctly in the response.

To circumvent this bug, the Z21Client handle this by requesting the protocol of the locomotive, and give you a 
correct response in the LocoInfoReceived event.

## Installation instructions

### Getting and trying put the Z21Client class

Download the repo and create a project in which to use the Z21Class. if you need inspiration, please see my project
'Z21Dashboard' on Github:

https://github.com/J-Wachs/Z21Dashboard

### Setting up your own project to use the Z21Client class

To use the Z21Client class in your own projects, you must add the component project to your solution. Then you must
add the 'Z21Client' to the Program.cs, or MauiProgram.cs, file of your project:


...
// Added for Z21Cleint
builder.Services.AddSingleton<IZ21Cleint, Z21Client>();
// End
...
```

## Modifying the Z21Clint for your own use

Maybe you need more information to be returned. Maybe you need to use a config value for some of the data returned.
Maybe you need one of the LocoNet or CAN bus commands/events.

Please feel free to adapt a local version to fit your needs.

## Found a bug?

Please create an issue in the repo.

## Known issues (Work in progress)

None at this time.

## FAQ

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

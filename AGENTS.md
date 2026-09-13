# Pokyny pro AI agenty

Tento soubor platí pro celý repozitář. Před změnou kódu si přečti příslušný
projekt a jeho závislosti; změny drž malé, modulární a ověřitelné.

## Struktura projektu

- `PurrplingCore.Toolkit/` obsahuje obecné herní utility, hosting, DI,
  konfiguraci, messaging, obsah, rendering a VFS. Nemá znát herní doménu.
- `PurrplingCore.Ecs/` je ECS integrační vrstva nad `Friflo.Engine.ECS`.
  Obsahuje world, factory, moduly, systémy, skupiny a ECS DI extension metody.
- `KittyCat/KittyCat.Core/` je herní doména: scény, služby, komponenty a
  systémy. Platformní projekty (`DesktopGL`, `WindowsDX`, `Android`, `iOS`)
  obsahují pouze platformní bootstrap a adaptace.
- Kořenový `KittyCat.sln` zahrnuje všechny projekty. Jednotlivé projekty cílí
  na `net8.0`; automatické testovací projekty v repozitáři aktuálně nejsou.

### Architektonická pyramida

Závislosti a odpovědnosti směřují shora dolů:

```text
Toolkit
  ↓
Engine
  ↓
Hra
```

- `PurrplingCore.Toolkit` je univerzální základ a framework pro stavbu enginu;
  nesmí znát konkrétní engine ani herní doménu.
- `PurrplingCore.Ecs` tvoří engine: poskytuje ECS world, factory, moduly,
  systémy a jejich DI integraci. Engine může využívat Toolkit.
- `KittyCat` je hra nad enginem. Může používat engine i univerzální nástroje
  z Toolkitu, ale herní logika nesmí být přesunuta do těchto nižších vrstev.

## Architektonická pravidla

### ECS first

- Herní stav a opakovaně prováděnou herní logiku modeluj nejdříve jako ECS
  komponenty, dotazy a systémy. Systémy registruj do správného worldu a groupy
  přes `IWorldBuilder`, `AddWorld` a `AddModule`.
- `World`, komponenty a systémy nesmí záviset na platformním bootstrapu.
  Platformní nebo UI integraci řeš přes malé rozhraní a DI službu.
- Běžný update loop nesmí vytvářet zbytečné objekty, provádět reflexi ani
  vyhledávat služby z `IServiceProvider`. Inicializaci a registraci proveď při
  startu hry; v hot path preferuj data-orientované ECS dotazy a předalokované
  kolekce.

### DI first

- Závislosti deklaruj v konstruktorech přes rozhraní. Nepoužívej globální stav,
  service locator ani ruční `new` pro aplikační služby.
- Registrace patří do `IServiceCollection` extension metod nebo do
  `IServicesConfiguration`; jednotlivé moduly mají registrovat pouze vlastní
  služby a systémy.
- Respektuj lifetime: singleton pro neměnnou/infrastrukturní službu, scoped pro
  `WorldContext` a world-scoped stav, transient pouze pro krátce žijící objekty.
  U ECS worldů vždy respektuj keyed registraci a jejich `WorldSignature`.
- Pro konfigurovatelné hodnoty používej options pattern (`IOptions<T>`), nikoli
  statické konfigurační proměnné. Registrace mají být deterministické a bezpečné
  pro opakované složení modulů (`TryAdd` tam, kde je to záměr).

### ASP.NET styl, ale herní výkon

- DI konfiguraci piš ve stylu ASP.NET: čitelné `AddXyz`/`UseXyz` extension
  metody, fluent composition, jasné lifetimes a oddělené options.
- Nepřenášej ale request/web middleware model do herního loopu. Herní frame,
  fixed-step update a render pipeline musí mít předvídatelný pořadník a nízkou
  režii.
- Reflexi, assembly scanning, validaci konfigurace a sestavení service provideru
  prováděj při bootstrapu, nikdy v každém framu. Preferuj statickou registraci,
  když je hot path citlivá na výkon.
- Každá nová abstrakce musí mít měřitelný přínos; při optimalizaci nejdříve
  zachovej čitelnost a ověř změnu benchmarkem nebo profilem, pokud je dostupný.

### Modularita

- Novou funkcionalitu umísti do nejmenšího odpovědného projektu a adresáře;
  nesdílej herní typy přes Toolkit.
- Veřejné API udržuj malé. Preferuj rozhraní, extension metody a samostatné
  moduly před úpravou centrálního bootstrapu.
- Změna pořadí ECS systémů musí být explicitní pomocí groupy nebo `SystemOrder`;
  nespoléhej na pořadí souborů či náhodné pořadí registrace.
- Každý systém musí být volitelný: registruj jej pouze přes explicitní modul,
  builder nebo konfiguraci a umožni jeho vypnutí bez úprav jádra enginu. Výchozí
  složení worldu nesmí aktivovat herní funkci skrytě nebo přes globální stav.
- Při změně veřejného API nebo DI registrace aktualizuj související dokumentaci
  a všechny platformní bootstrapy.

## Pracovní postup

1. Prozkoumej existující extension metody, lifetimes a ECS groupy, než přidáš
   novou konvenci.
2. Zachovej nullable anotace a styl existujících C# souborů; nepřidávej
   nesouvisející refaktoring.
3. Po změně spusť cíleně `dotnet build KittyCat.sln` (případně build
   konkrétního projektu). Pokud přidáš testovací projekt, spusť jeho
   `dotnet test`.
4. Před odevzdáním zkontroluj `git diff`, že nejsou zahrnuté build artefakty,
   lokální konfigurace ani tajné údaje.

# mechaniX

Eine WPF-Anwendung zur Berechnung der Auflagerreaktionen eines einfach gelagerten Trägers mit bis zu fünf Einzelkräften.

## Funktionen

- Stützweite `l` und eine bis fünf Einzelkräfte eingeben
- Für jede Kraft Betrag `F`, Position `x` ab Lager A und Winkel festlegen
- Auflagerreaktionen am Festlager A und Loslager B berechnen
- System und Kraftrichtungen schematisch anzeigen
- Eingaben mit Dezimalkomma oder Dezimalpunkt verwenden

## Eingaben und Vorzeichen

- Längen werden in Metern (`m`), Kräfte in Kilonewton (`kN`) eingegeben.
- Die Kraftposition muss zwischen den Lagern liegen: `0 ≤ x ≤ l`.
- Der Winkel wird von der horizontalen Trägerachse aus gemessen: `0°` zeigt nach rechts, `90°` nach oben. Positive Winkel werden gegen den Uhrzeigersinn gemessen.
- Der Kraftbetrag muss größer als null sein. Für Kräfte nach unten kann beispielsweise `-90°` eingegeben werden.
- Positive Reaktionskomponenten zeigen nach rechts beziehungsweise nach oben; negative Werte zeigen in die entgegengesetzte Richtung.
- Das Festlager A nimmt horizontale und vertikale Kräfte auf. Das Loslager B überträgt nur eine vertikale Kraft; seine horizontale Reaktion ist null.

## Berechnung

Jede Einzelkraft wird in ihre horizontalen und vertikalen Komponenten zerlegt:

```text
Fₓ = F · cos(α)
Fᵧ = F · sin(α)
```

Die Reaktionen folgen aus dem Kräfte- und Momentengleichgewicht:

```text
Aₓ = −ΣFₓ
Bᵧ = −Σ(x · Fᵧ) / l
Aᵧ = −ΣFᵧ − Bᵧ
```

Das Modell berücksichtigt statische Einzelkräfte auf einem geraden, horizontalen Träger. Streckenlasten, eingeprägte Momente, Eigengewicht und Verformungen werden nicht berücksichtigt.

## Starten und bauen

Voraussetzung ist Windows mit dem .NET 11 SDK und den WPF-Referenzpaketen. Das Projekt verwendet derzeit .NET 11 Preview.

Im Repository-Verzeichnis:

```powershell
dotnet build mechaniX.slnx
dotnet run --project mechaniX\mechaniX.csproj
```

Alternativ kann `mechaniX.slnx` in JetBrains Rider geöffnet und das Projekt dort gestartet werden.

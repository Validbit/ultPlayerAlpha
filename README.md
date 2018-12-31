# ultPlayerAlpha
## Popis
Zde se nachází zdrojový kód pro experimentální hudební přehrávač ultPlayer, který je cílem absoloventské práce Microsoft STC ročníku 2017. 
*Zatím však není určen pro veřejné přispívání*
(Pokud by vás projekt zaujal, kontaktujte mě na email uvedený na GitHubu. Rád odpovím na vaše otázky)

Pro zajištění správné funkconality
- Nastavte konfiguraci pro Build na **Release x86** 
- Po úspěšném zakončení Build fáze spusťte program **z nabídky Start**

## Známé chyby
### Příliš zasekaný program
Po přechodu z Debug verze do Release se začalo ladění Visual Studia sekat aplikaci při interakcích s Windows. Tomuto chování se dá vyvarovat **Spuštěním aplikace z nabídky Start (po úspěšném zakončení Build)**.
Pokud ani to nevyjde, zkuste:
1. VS > Build > Clean Solution
2. VS > Build > Rebuild solution
3. (po automatickém spuštění aplikace po Rebuild) Zavřít Visual Studio
4. Spustit aplikaci z nabídky Start
### Po kliknutí na tlačítko se panel neotevře
Jelikož je aplikace ve vývojové fázi nazvané jako Alpha, není ještě mnoho funkcí správně implementováno a tak jsou na místech pouze přístupová tlačítka.

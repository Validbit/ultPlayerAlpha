# ultPlayerAlpha
## Popis
Zde se nachází zdrojový kód pro experimentální hudební přehrávač ultPlayer, který je cílem absoloventské práce Microsoft STC ročníku 2017
## Známé chyby
### Příliš zasekaný program
Po přechodu z Debug verze do Release se začalo ladění Visual Studia sekat aplikaci při interakcích s Windows. Tomuto chování se dá vyvarovat:
1. VS > Build > Clean Solution
2. VS > Build > Rebuild solution
3. (po automatickém spuštění aplikace po Rebuild) Zavřít Visual Studio
4. Spustit aplikaci z nabídky Start
### Po kliknutí na tlačítko se panel neotevře
Jelikož je aplikace ve vývojové fázi nazvané jako Alpha, není ještě mnoho funkcí správně implementováno a tak jsou na místech pouze přístupová tlačítka.

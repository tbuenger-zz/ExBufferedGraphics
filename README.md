Beschreibung
============

Für zwei Programme an denen ich gerade arbeite, hat die normale Leistung der GDI-Operationen, die von Graphics angeboten wurden, nicht ausgereicht.
Doch zum Glück gibt es die [BufferedGraphics](http://msdn.microsoft.com/de-de/library/system.drawing.bufferedgraphics.aspx)-Klasse im .NET-Framework, die intern einen Bild-Puffer implementiert, den man blitzschnell (mittels der intern genutzten  [BitBlt](http://msdn.microsoft.com/en-us/library/dd183370%28VS.85%29.aspx)-Funktion) zeichnen kann.

Bisherige Probleme
------------------
Doch war die Verwendung für meine Einsatzzwecke aus mehreren Gründen sehr unkomfortabel:

* Für das korrekte Arbeiten braucht man nicht nur BufferedGraphics-Objekte sondern auch BufferedGraphicsContext-Objekte, also muss man immer mit 2 Klassen hantieren.
* Ein Resizen (aufgrund von nem OnResize des Panels) ist relativ mühsam und erfordert einiges an Code
* Das Zeichnen mittels [BufferedGraphics.Render(...)](http://msdn.microsoft.com/de-de/library/system.drawing.bufferedgraphics.render.aspx) bietet keinerlei Einstellung, WO gezeichnet wird, obwohl BitBlit dies anbietet.

Vorteile mit diesem Snippet
---------------------------
Die neue Klasse heißt ExBufferedGraphics (Extended) und kapselt dies alles noch einmal und bietet somit viele Vorteile:

* Nur einmal muss so ein Objekt angelegt werden, wobei man dem Konstruktor ein Referenz-Graphics Objekt übergibt (von dem der Buffer die Device-Eigenschaften übernimmt) oder auch direkt ein Control angibt, auf das man später zeichnen will. 
* Da beim Resizing des Controls meist auch der Buffer mitverändert wird, kann man die Size-Eigenschaft ändern. Beim Verkleinen wird nie neuer Speicher angefordert, sondern nur einfach ein Ausschnitt des bisherigen Buffers weiterverwendet.
Und beim Vergrößern wird - ähnlich zur List-Implementierung - der Buffer lieber gleich etwas mehr vergrößert: Auf den doppelten Speicher, also pro Dimension um sqrt(2). So wird ein ständiges ReAllozieren beim langsamen "Groß-Ziehen" eines Controls verhindert.
* Beim Zeichnen kann nun auch eine Verschiebung angegeben werden, sodass der Buffer versetzt gezeichnet wird.

Mögliche Einsatzgebiete
-----------------------
Ich selbst verwende die Klasse für folgendes:

* **Lupen-Controls**
Ich habe ein großes Control, das ein (Übersichts-)Bild darstellt und drum herum einige kleine, die ausschnitte zeigen. Nun kann ich auf dem Übersichtsbild die Lupenausschnitte verschieben. Damit das Verschieben auch smooth läuft, auch wenn alle Lupen gleichzeitig verschiebt und trotzdem eine Live-Vorschau sehen will, verbietet sich ein x-faches Graphics.DrawImage(...) in den einzelnen Lupen-Controls bei jedem MouseMove.
Alles stockt, Speicher wird alloziert/freigegeben, nichts ist flüssig ! 
Nun verwende ich diese Klasse in der ich das Bild einmal Groß hineinzeichne. Und die Lupen-Controls Rendern nur noch diese ExBufferedGraphics (was intern mittels BitBlt viel viel schneller geht) am gewünschten Ausschnitt.
Nicht nur, dass es "performanter" geht, es geht nun überhaupt !

* **Drag'n'Drop in CustomControls**
Hat man aber hunderte von Objekte und sogar Bilder dabei, dann wird dass komplette Neuzeichnen in der OnPaint-Methode zum Flaschenhals. Das Draggen ist nicht mehr flüssig und die Anwendung macht keinen Spaß.
Deshalb verwende ich nun "2 Ebenen". 
Die Szenerie an sich rendere ich stets in einem ExBufferedGraphics, den ich mit OnPaint nur noch auf das Control blitte (damit hab ich auch zugleich ein custom-double-buffering implementiert).
Wähle ich nun ein Objekt zum Verschieben aus, render ich noch einmal die Szenerie ohne das Objekt in den Buffer und dies wird nun mein Hintergrund, den ich ab sofort nur noch auf das Panel blitte. Das ausgewählte Objekt zeichne ich anschließend direkt oben drauf.
Der Hintergrund wird also eingefroren in einen Buffer und nur noch geblittet. Und nur noch das gerade verschobene Objekt wird "gezeichnet".
(Wenn es sich dabei um ein Bild handelt, kann man natürlich auch dieses nochmal in ein ExBufferedGraphics zeichnen und nur noch verschoben blitten. Aber für einfache Graphiken ist es überflüssig)
So wird auch das Verschieben von Objekte in komplexen Szenen wieder flüssig.

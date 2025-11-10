using System;
using System.Collections.Generic;
using iText.Kernel.Geom;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Pdf.Canvas.Parser.Listener; // EventType

namespace PdfChecker.API.Services
{
    // Simple event-listener that collects text chunks and their bounding rectangles
    public class MyLocationStrategy : IEventListener
    {
        public List<(string Text, Rectangle Rect)> Chunks { get; } = new();

        // Called by PdfCanvasProcessor for each rendering event
        public void EventOccurred(IEventData data, EventType type)
        {
            if (type != EventType.RENDER_TEXT) return;

            var renderInfo = (TextRenderInfo)data;

            try
            {
                // Get baseline/ascent/descent to build a rectangle
                var baseline = renderInfo.GetBaseline();
                var descent = renderInfo.GetDescentLine();
                var ascent = renderInfo.GetAscentLine();

                var start = baseline.GetStartPoint();
                var descentStart = descent.GetStartPoint();
                var ascentEnd = ascent.GetEndPoint();

                float x = start.Get(0);
                float y = descentStart.Get(1);
                float width = ascentEnd.Get(0) - x;
                float height = ascentEnd.Get(1) - descentStart.Get(1);

                // Defensive: ensure positive width/height
                if (width <= 0) width = Math.Abs(width);
                if (height <= 0) height = Math.Abs(height);

                var rect = new Rectangle(x, y, width, height);
                Chunks.Add((renderInfo.GetText() ?? string.Empty, rect));
            }
            catch
            {
                // swallow any chunk we cannot parse
            }
        }

        // Limit to text render events only
        public ICollection<EventType> GetSupportedEvents()
        {
            return new List<EventType> { EventType.RENDER_TEXT };
        }
    }
}

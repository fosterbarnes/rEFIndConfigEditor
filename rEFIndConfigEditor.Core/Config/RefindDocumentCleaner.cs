namespace rEFIndConfigEditor.Config;

public static class RefindDocumentCleaner
{
    public static void StripComments(RefindDocument doc)
    {
        for (var i = doc.Rows.Count - 1; i >= 0; i--)
        {
            switch (doc.Rows[i])
            {
                case GlobalOptionRow g when !g.Option.IsActive:
                    doc.Rows.RemoveAt(i);
                    break;
                case RawConfigRow raw when IsCommentOrBlank(raw.Text):
                    doc.Rows.RemoveAt(i);
                    break;
            }
        }
    }

    private static bool IsCommentOrBlank(string text)
    {
        var trimmed = text.Trim();
        return trimmed.Length == 0 || trimmed.StartsWith('#');
    }
}

namespace OpenHdWebUi.Server.Configuration;

public class DocumentationConfiguration
{
    /// <summary>
    /// Absolute or relative path to the folder that contains the rendered documentation assets.
    /// The path is resolved during runtime. If it is empty the bundled fallback documentation is used.
    /// </summary>
    public string? LocalDocsRoot { get; set; }

    /// <summary>
    /// Relative URL path under which the documentation will be exposed. Defaults to "/docs".
    /// </summary>
    public string RequestPath { get; set; } = "/docs";

    /// <summary>
    /// Relative path (within <see cref="LocalDocsRoot"/>) to the introduction page that should be opened from the UI.
    /// Defaults to "introduction/index.html" which matches both the bundled fallback and the generated Docusaurus build.
    /// </summary>
    public string LocalIntroPage { get; set; } = "introduction/index.html";
}

using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace PurrplingCore.ContentBuilder.Xml;

internal static class ContentSourceParser
{
    public static ContentModel LoadContentModel(string inputFile)
    {
        XmlSerializer serializer = new(typeof(ContentModel));
        using var stream = new FileStream(inputFile, FileMode.Open, FileAccess.Read);
        ContentModel? content = (ContentModel?)serializer.Deserialize(stream);

        if (content is not null)
            return content;

        throw new InvalidOperationException("Failed to deserialize content model.");
    }
}

public record ContentModel
{
    [XmlAttribute("src")]
    public required string Src { get; init; }

    [XmlAttribute("dest")]
    public required string Dest { get; init; }
    
    [XmlElement("Bundle")]
    public required IList<Bundle> Bundles { get; init; }
}

public record class Bundle
{
    [XmlAttribute("type")]
    public required string Type { get; set; }

    [XmlAttribute("src")]
    public required string Src { get; set; }

    [XmlAttribute("dest")]
    public required string Dest { get; set; }

    // Různé elementy vkládáme do jednoho seznamu pomocí Base class
    [XmlElement("Compile", typeof(Compile))]
    [XmlElement("Copy", typeof(Copy))]
    public List<BundleItem> Items { get; set; } = [];
}

public abstract record class BundleItem { }

public record class Compile : BundleItem
{
    [XmlAttribute("asset")]
    public required string Asset { get; set; }

    [XmlAttribute("importer")]
    public required string Importer { get; set; }

    [XmlAttribute("processor")]
    public required string Processor { get; set; }
}

public record class Copy : BundleItem
{
    [XmlAttribute("asset")]
    public required string Asset { get; set; }

    [XmlAttribute("rule")]
    public required string Rule { get; set; }

    [XmlAttribute("dest")]
    public required string Dest { get; set; }
}

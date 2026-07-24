[Documentation](../README.md) · [User quick start](quick-start.md) · **User how-to** · [User concepts](concepts.md)

# How to

## Link/Unlink a file format to/from a viewer

|                                                                        |                                                                                                                                                                                                                         |
| ---------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| ![Thesaurus initial XML links](../images/thesaurus-proc-01.png)        | We start with a thesaurus where "XMLFile" is a child of "Text", which is itself a child of "AnyFile", and a parent of "SVGFile".                                                                                        |
| ![XML first Analysis](../images/thesaurus-proc-02.png)                 | Analyzing a file detected as "XML" displays the properties associated with "AnyFile", as well as the AvalonEdit viewer associated with "Text". At this stage, no viewer is associated directly with XML.                |
| ![Thesaurus update](../images/thesaurus-proc-03.png)                   | Return to the thesaurus. In the left tree, drag and drop "XMLFile" onto "WebView2". "XMLFile" then also becomes a child of "WebView2". Note that this also affects the children of "XMLFile", including "SVGFile" here. |
| ![XML second Analysis](../images/thesaurus-proc-04.png)                | Analyzing an XML file now also activates the viewer associated with "WebView2".                                                                                                                                         |
| ![SVG first Analysis WebView2](../images/thesaurus-proc-05.png)        | Analyzing an SVG file also activates the viewer associated with "WebView2".                                                                                                                                             |
| ![SVG first Analysis ImageMagick](../images/thesaurus-proc-06.png)     | Another viewer, ImageMagick, is also associated with SVG. We will disable this viewer for files identified as "SVGFile".                                                                                                |
| ![Thesaurue delete parent-child link](../images/thesaurus-proc-07.png) | Select "SVGFile" in the left tree. Then, in the central panel, click the trash icon in the Parents section to remove the relationship between "SVGFile" and "ImageMagick viewer".                                       |
| ![Thesaurue SVG link tree](../images/thesaurus-proc-08.png)            | "SVGFile" now has only one parent: "XMLFile".                                                                                                                                                                           |
| ![SVG second Analysis](../images/thesaurus-proc-09.png)                | Analyzing an SVG file no longer activates the ImageMagick viewer. As a child of "XMLFile", it still activates the "WebView2" viewer.                                                                                    |

---

[Documentation home](../README.md) · [Previous: User quick start](quick-start.md) · [Next: User concepts](concepts.md)

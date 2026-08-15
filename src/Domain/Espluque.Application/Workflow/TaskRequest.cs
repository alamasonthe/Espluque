using Espluque.Contracts.Workflow;

namespace Espluque.Application.Workflow
{
    /// <summary>
    /// Represents an analysis task associated with a thesaurus tag.
    /// </summary>
    /// <remarks>
    /// Used by AnalysisEngine in the viewer and grabber backlogs.
    /// Tag is a preferred thesaurus term used to select the matching contributions; Status tracks whether that backlog entry still has to be processed.
    /// Tasks are added from the current file format and its ancestor concepts, starting with AnyFile.
    /// </remarks>

    public class TaskRequest
    {
        public string Tag { get; set; }

        public TaskStatusEnum Status { get; set; } = TaskStatusEnum.ToDo;
    }
}

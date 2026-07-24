using Espluque.Contracts.Enums;

namespace Espluque.Application.Entities
{
    public class TaskRequest
    {
        public string Tag { get; set; }

        public TaskStatusEnum Status { get; set; } = TaskStatusEnum.ToDo;
    }
}

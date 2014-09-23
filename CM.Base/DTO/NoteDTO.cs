using System;

namespace CM.Shared.DTO
{
    /// <summary>
    /// DTO used to register a new user.
    /// </summary>
    public class NoteDTO
    {
        public string Text { get; set; }
        public string Title { get; set; }
    }

    public class NoteReadDTO : NoteDTO
    {
        public string Id { get; set; }
        public DateTime Created { get; set; }
    }
}

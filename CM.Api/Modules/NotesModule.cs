using System.Linq;
using AutoMapper;
using CM.Base.BusinessModels;
using CM.Base.DBServices;
using CM.Shared.DTO;
using MongoDB.Bson;
using Nancy;
using Nancy.ModelBinding;

namespace CM.Api.Modules
{
    public class NotesModule : BaseModule
    {
        public NotesModule(NotesDBService notesService)
        {
            Get["/api/v1/notes/"] = _ =>
            {
                var results = notesService.All();
                return results.Select(p => AutoMapper.Mapper.DynamicMap<NoteReadDTO>(p));
            };

            Get["/api/v1/notes/{noteId}"] = _ =>
            {
                var result = notesService.SingleByIdFromUser(_.noteId);
                return AutoMapper.Mapper.DynamicMap<NoteReadDTO>(result);
            };

            Put["/api/v1/notes/{noteId}"] = _ =>
            {
                ObjectId noteId = ObjectId.Parse(_.noteId);
                var dto = this.Bind<NoteDTO>();
                var note = notesService.SingleById(noteId);
                Mapper.DynamicMap(dto, note);
                notesService.Update(note);
                return Mapper.DynamicMap<NoteReadDTO>(note);
            };

            Delete["/api/v1/notes/{noteId}"] = _ =>
            {
                ObjectId noteId = ObjectId.Parse(_.noteId);
                notesService.Delete(noteId);
                return new Response().WithStatusCode(HttpStatusCode.NoContent);
            };

            Post["/api/v1/notes/"] = _ =>
            {
                var dto = this.Bind<NoteDTO>();
                var note = Mapper.DynamicMap<Note>(dto);
                note = notesService.Create(note);
                return Response.AsJson(Mapper.DynamicMap<NoteReadDTO>(note), HttpStatusCode.Created);
            };
        }
    }
}

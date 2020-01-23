using System;
using System.Collections.Generic;
using System.Linq;
using LiteDB;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using restapi.Helpers;

namespace restapi.Models
{
    public class Timecard
    {
        public Timecard() { }

        public Timecard(int person)
        {
            Opened = DateTime.UtcNow;
            Employee = person;
            UniqueIdentifier = Guid.NewGuid();
            Lines = new List<TimecardLine>();
            Transitions = new List<Transition>();
        }

        public int Employee { get; private set; }

        public TimecardStatus Status
        {
            get
            {
                return Transitions
                    .OrderByDescending(t => t.OccurredAt)
                    .First()
                    .TransitionedTo;
            }
        }

        [BsonIgnore]
        [JsonProperty("_self")]
        public string Self { get => $"/timesheets/{UniqueIdentifier}"; }

        public DateTime Opened { get; set; }

        [JsonIgnore]
        [BsonId]
        public ObjectId Id { get; set; }

        [JsonProperty("id")]
        public Guid UniqueIdentifier { get; set; }

        [JsonIgnore]
        public IList<TimecardLine> Lines { get; set; }

        [JsonIgnore]
        public IList<Transition> Transitions { get; set; }

        public IList<ActionLink> Actions { get => GetActionLinks(); }

        [JsonProperty("documentation")]
        public IList<DocumentLink> Documents { get => GetDocumentLinks(); }

        public string Version { get; set; } = "timecard-0.1";

        private IList<ActionLink> GetActionLinks()
        {
            var links = new List<ActionLink>();

            switch (Status)
            {
                case TimecardStatus.Draft:
                    links.Add(new ActionLink()
                    {
                        Method = Method.Post,
                        Type = ContentTypes.Cancellation,
                        Relationship = ActionRelationship.Cancel,
                        Reference = $"/timesheets/{UniqueIdentifier}/cancellation"
                    });

                    links.Add(new ActionLink()
                    {
                        Method = Method.Post,
                        Type = ContentTypes.Submittal,
                        Relationship = ActionRelationship.Submit,
                        Reference = $"/timesheets/{UniqueIdentifier}/submittal"
                    });

                    links.Add(new ActionLink()
                    {
                        Method = Method.Post,
                        Type = ContentTypes.TimesheetLine,
                        Relationship = ActionRelationship.RecordLine,
                        Reference = $"/timesheets/{UniqueIdentifier}/lines"
                    });

                    break;

                case TimecardStatus.Submitted:
                    links.Add(new ActionLink()
                    {
                        Method = Method.Post,
                        Type = ContentTypes.Cancellation,
                        Relationship = ActionRelationship.Cancel,
                        Reference = $"/timesheets/{UniqueIdentifier}/cancellation"
                    });

                    links.Add(new ActionLink()
                    {
                        Method = Method.Post,
                        Type = ContentTypes.Rejection,
                        Relationship = ActionRelationship.Reject,
                        Reference = $"/timesheets/{UniqueIdentifier}/rejection"
                    });

                    links.Add(new ActionLink()
                    {
                        Method = Method.Post,
                        Type = ContentTypes.Approval,
                        Relationship = ActionRelationship.Approve,
                        Reference = $"/timesheets/{UniqueIdentifier}/approval"
                    });

                    break;

                case TimecardStatus.Approved:
                    // terminal state, nothing possible here
                    break;

                case TimecardStatus.Cancelled:
                    // terminal state, nothing possible here
                    break;
            }

            return links;
        }

        private IList<DocumentLink> GetDocumentLinks()
        {
            var links = new List<DocumentLink>();

            links.Add(new DocumentLink()
            {
                Method = Method.Get,
                Type = ContentTypes.Transitions,
                Relationship = DocumentRelationship.Transitions,
                Reference = $"/timesheets/{UniqueIdentifier}/transitions"
            });

            if (this.Lines.Count > 0)
            {
                links.Add(new DocumentLink()
                {
                    Method = Method.Get,
                    Type = ContentTypes.TimesheetLine,
                    Relationship = DocumentRelationship.Lines,
                    Reference = $"/timesheets/{UniqueIdentifier}/lines"
                });
            }

            if (this.Status == TimecardStatus.Submitted)
            {
                links.Add(new DocumentLink()
                {
                    Method = Method.Get,
                    Type = ContentTypes.Transitions,
                    Relationship = DocumentRelationship.Submittal,
                    Reference = $"/timesheets/{UniqueIdentifier}/submittal"
                });
            }

            return links;
        }

        public TimecardLine AddLine(DocumentLine documentLine)
        {
            var annotatedLine = new TimecardLine(documentLine);

            Lines.Add(annotatedLine);

            return annotatedLine;
        }

        public TimecardLine ReplaceLine(Guid lineId, DocumentLine documentLine)
        {
            var lineToReplace = Lines.First(l => l.UniqueIdentifier == lineId);

            var replacedLine = new TimecardLine(documentLine);

            replacedLine.UniqueIdentifier = lineId;

            if (Lines.Remove(lineToReplace))
            {
                Lines.Add(replacedLine);
            }

            return replacedLine;
        }

        public TimecardLine UpdateLine(Guid lineId, JObject request)
        {
            var lineToUpdate = Lines.First(l => l.UniqueIdentifier == lineId);

            lineToUpdate.Update(request);

            return lineToUpdate;
        }

        public bool CanBeDeleted()
        {
            return (Status == TimecardStatus.Cancelled || Status == TimecardStatus.Draft);
        }

        public bool HasLine(Guid lineId)
        {
            return Lines
                .Any(l => l.UniqueIdentifier == lineId);
        }


        public override string ToString()
        {
            return PublicJsonSerializer.SerializeObjectIndented(this);
        }
    }
}

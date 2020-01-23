using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using restapi.Models;

namespace restapi.Controllers
{
    public class RootController : Controller
    {
        // GET api/values
        [Route("~/")]
        [HttpGet]
        [Produces(ContentTypes.Root)]
        [ProducesResponseType(typeof(IDictionary<ApplicationRelationship, object>), 200)]
        public IDictionary<ApplicationRelationship, object> Get()
        {
            return new Dictionary<ApplicationRelationship, object>()
            {
                {
                    ApplicationRelationship.Timesheets, new List<DocumentLink>()
                    {
                        new DocumentLink()
                        {
                            Method = Method.Get,
                            Type = ContentTypes.Timesheets,
                            Relationship = DocumentRelationship.Timesheets,
                            Reference = "/timesheets"
                        },
                        new DocumentLink()
                        {
                            Method = Method.Post,
                            Type = ContentTypes.Timesheets,
                            Relationship = DocumentRelationship.Timesheets,
                            Reference = "/timesheets"
                        }
                    }
                },
                {
                    ApplicationRelationship.Version, "0.1"
                }
            };
        }

        [HttpPost]
        [Produces(ContentTypes.Root)]
        [ProducesResponseType(typeof(Timecard), 200)]
        public Timecard CreateTimecard([FromBody] DocumentPerson person)
        {
            //logger.LogInformation($"Creating timesheet for {person.ToString()}");

            var timecard = new Timecard(person.Id);

            var entered = new Entered() { Person = person.Id };

            timecard.Transitions.Add(new Transition(entered));

            return timecard;
        }
    }
}

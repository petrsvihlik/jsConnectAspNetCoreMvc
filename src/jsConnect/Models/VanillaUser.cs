using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace jsConnect.Models
{
    /// <summary>
    /// A lightweight .NET representation of the API user response
    /// </summary>
    public class VanillaUser
    {
        public VanillaUserProfile Profile { get; set; }
    }

    public class VanillaUserProfile
    {
        public int? UserID { get; set; }
        public string Name { get; set; }
        public string Photo { get; set; }
        public object Title { get; set; }
        public string Location { get; set; }
        public string About { get; set; }
        public int? CountVisits { get; set; }
        public string DateFirstVisit { get; set; }
        public string DateLastActive { get; set; }
        public string DateInserted { get; set; }
        public string DateUpdated { get; set; }
        public object Score { get; set; }
        public int? Deleted { get; set; }
        public int? Points { get; set; }
        public int? CountUnreadConversations { get; set; }
        public int? CountDiscussions { get; set; }
        public object CountUnreadDiscussions { get; set; }
        public int? CountComments { get; set; }
        public int? CountAcceptedAnswers { get; set; }
        public int? CountBadges { get; set; }
        public int? RankID { get; set; }
        public string PhotoUrl { get; set; }
        public string _CssClass { get; set; }
        public bool Online { get; set; }
        public object LastOnlineDate { get; set; }
    }
}

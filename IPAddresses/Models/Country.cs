using System;
using System.Collections.Generic;

namespace IPAddresses.Models;

public partial class Country
{
    public Country()
    {
        this.Ipaddresses = new HashSet<Ipaddress>();
    }
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string TwoLetterCode { get; set; } = null!;

    public string ThreeLetterCode { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Ipaddress> Ipaddresses { get; set; }

}

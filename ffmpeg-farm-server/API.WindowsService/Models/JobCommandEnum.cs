using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace API.WindowsService.Models
{
    public enum Command
    {
        Unknown = 0,
        Pause,
        Resume,
        Cancel
    }
}

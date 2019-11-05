using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwinCatAdsTool.Interfaces.Models
{
    public class NetId
    {
        private string name;
        private string address;

        public string Address
        {
            get => address;
            set => address = value;
        }

        public string Name
        {
            get => name;
            set => name = value;
        }
    }
}

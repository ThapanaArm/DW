using System.ComponentModel;
using System;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace BIS_INTERFACE_BC
{
    public partial class Service1 : ServiceBase
    {
        public Service1()
        {
           // InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
        }

        protected override void OnStop()
        {
        }
    }
}

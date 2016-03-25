using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WZ.Common
{
    public class JsonState
    {
        public JsonState()
        {
            this.state = 0;
            this.info = "操作成功";
        }
        public JsonState(int error=0)
        {
            if (error == 0)
            {
                this.state = 0;
                this.info = "操作成功";
            }
            else
            {
                this.state = 99;
                this.info = "操作失败";
            }
        }
        public int state { get; set; }
        public string info { get; set; }
    }

    public class JsonTable : JsonState
    {
        public long draw { get; set; }
        public object data { get; set; }
        public long recordsTotal { get; set; }
        public long recordsFiltered { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using MessagePack;

namespace Common
{
    [MessagePackObject]
    public class Req
    {

        [Key(0)]
        public int Id { get; set; }

    }

    [MessagePackObject]
    public class RegisterReqMsg : Req
    {


        [Key(1)]
        public int Phone { get; set; }


        [Key(2)]
        public string Authcode { get; set; }
    }
}

using Common.Models;
using Common.Parsers.Json;
using Common.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Common.Data
{


    public interface IClient
    {
        long Id { get; }
        string FirstName { get; set; }
        string FullName { get; }
        string LastName { get; set; }
        bool Check(out string error);
        void CloneFrom(IClient c);
    }

    public interface ILogin
    {
        long Id { get; set; }
        IClient Client { get; set; }
        string EncPwd { get; set; }
        string Identification { get; set; }
        bool IsLogged { get; set; }
        bool IsThrusted { get; set; }
        bool IsValidated { get; set; }
        int Permission { get; set; }
        string Pwd { get; set; }
        string Username { get; set; }

        bool Check();
        bool Check(out List<JObject> k);
        bool Check(RequestArgs args);
        bool Check(RequestArgs args, ref List<JObject> k);
        User InitUser();
        JValue Parse(JValue json);
        bool RegeneratePwd(string keyString);
        int Repaire(DataBaseStructure db);
    }
}

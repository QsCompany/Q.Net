using Common.Attributes;
using Common.Binding;
using Common.Parsers;
using Common.Parsers.Json;
using Common.Services;
using System;
using System.Collections.Generic;
using System.Linq;

using System.Text;

namespace Common.Models
{

    [HosteableObject(null, typeof(Serializers.NegociationResponse))]
    public class NegociationResponse : DObject
    {
        public static int DPId = Register<NegociationResponse, Guid>("Id");
        public static int DPResponse = Register<NegociationResponse, JObject>("Response");
        public Guid Id => get<Guid>(DPId);
        public JObject Response => get<JObject>(DPResponse);

        public NegociationResponse()
        {
        }
        public NegociationResponse(Context c, JValue v)
        {
            set(DPId, v["Id"]);
            set(DPResponse, v["Response"]);
        }
    }

    sealed public class NegociationRequest : __Service__
    {
        public static int DPId = Register<NegociationRequest, Guid>("Id");
        public Guid Id => get<Guid>(DPId);
        public JObject Request => (JObject)get(DPserviceData);

        public NegociationRequest()
            : base("native_confirmation")
        {
            dropRequest = true;
        }
    }

    sealed public class SecurityAccountRequest : __Service__
    {
        public new static int __LOAD__(int dp) => DPWait;
        public static int DPOriginalIP = Register<SecurityAccountRequest, string>("OriginalIP");
        public static int DPYourIP = Register<SecurityAccountRequest, string>("YourIP");
        public static int DPWait = Register<SecurityAccountRequest, int>("Wait");

        public JObject RequestData
        {
            get => (JObject)get(DPserviceData);
            set => set(DPrequestData, value);
        }
        public JObject ServiceData
        {
            get => (JObject)get(DPserviceData);
            set => set(DPserviceData, value);
        }

        public string OriginalIP
        {
            get => (string)get(DPOriginalIP);
            set => set(DPOriginalIP, value);
        }
        public string YourIP
        {
            get => (string)get(DPYourIP);
            set => set(DPYourIP, value);
        }
        public int Wait
        {
            get => (int)get(DPWait);
            set => set(DPWait, value);
        }



        public SecurityAccountRequest()
            : base(nameof(SecurityAccountRequest))
        {
            dropRequest = true;
        }
    }

    public class Negociation
    {
        public Guid Id;
        public User user;
        public Object Data;
        public DateTime ExpireDate;
        public Action<Negociation, RequestArgs> OnResponse;
    }
}
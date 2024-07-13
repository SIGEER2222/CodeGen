using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FluentSyntaxRewriter.Test.Help;
public static class StringCode {
  private const string AttributeSourceCode = $$$"""
        // <auto-generated/>

        #nullable enable

        namespace {{Namespace}};

        [global::System.AttributeUsage(global::System.AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
        public sealed class {{GenerateAttributeName}}<TItem> : System.Attribute
            where TItem : class
        {
            public {{GenerateAttributeName}}(string filepath)
            {
                this.Filepath = filepath;
            }

            public string Filepath { get; }
            
            public string Key { get; set; } = "Id";

            public bool Group { get; set; }
        }

        [global::System.AttributeUsage(global::System.AttributeTargets.Class | global::System.AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
        public sealed class {{ExtendAttributeName}} : System.Attribute
        {
            public {{ExtendAttributeName}}(string masterAssetName, string filepath)
            {
                this.MasterAssetName = masterAssetName;
                this.Filepath = filepath;
            }
            
            public string MasterAssetName { get; }
            
            public string Filepath { get; }
            
            public string? FeatureFlag { get; set; }
        }
        """;
  public static readonly string GeneratedCopyText = """
  /// <summary>
  /// </summary>
  public partial class static __TypeName__   {
    private static __Type__ CopyTo(this __TypeName__ source, __TypeName__ target) {
      target.SchedName = source.SchedName;
      target.TriggerName = source.TriggerName;
      target.TriggerGroup = source.TriggerGroup;
      target.BlobData = source.BlobData;
      target.MomjobTrigger = source.MomjobTrigger;
      return target;
    }
  }
  """;
  public static readonly string CopyText = """
  using System;
  using System.Linq;
  namespace ManuTalent.Mom.Semi.FrontEnd.BusinessRules.TxnRule.WIP.WaferStart;

  [BusinessRuleValidatorType(typeof(CreateLotConfigValidator), typeof(CreateLotPlanProductValidator))]
  public class CreateLot : SemiFrontEndAdhocBusinessRuleBase
  {
      [BusinessRule(SemiFrontEndAccessStringConsts.OperationTxnWIPWaferStartCreateLot)]
      [BusinessRuleLockProperty(LockPrefix = nameof(CreateLot), LockProperty = nameof(CreateLotRuleBusinessRuleRequest.FABName), Order = 1)]
      [BusinessRuleLockProperty(LockPrefix = nameof(CreateLot), LockProperty = nameof(CreateLotRuleBusinessRuleRequest.ProductName), Order = 2)]
      public async Task<SemiFrontEndAdhocRuleResponseBase> DoTxn(CreateLotRuleBusinessRuleRequest request)
      {
          var lots = await WaferStartFacade.CreateLotsAsync(
                  request.FABName, request.ProductName,
                  request.LotType, request.LotCount, request.Qty, request.UserName,
                  request.ReasonCode, request.CommentDetail, request.StartHold, request.Department,
                  request.Section, request.Holder, request.HolderTel, request.Sponsor,
                  request.SponsorTel, request.HoldComment
              );
          var sCreateLotList = lots.Select(x => x.Name).Aggregate((x, y) => x + Environment.NewLine + y);
          var holdLotReqJson = string.Empty;

          foreach (var lot in lots)
          {
              if (request.StartHold)
              {

                var holdLotReq = new HoldLotRequest();
                request.CopyTo(holdLotReq);

                  var holdLotReq = new HoldLotRequest()
                  {
                      LotName = lot.Name,
                      Department = request.Department,
                      Section = request.Section,
                      Holder = request.Holder,
                      HolderTel = request.HolderTel,
                      Sponsor = request.Sponsor,
                      SponsorTel = request.SponsorTel,
                      ReasonCode = request.ReasonCode,
                      UserName = request.UserName,
                      CommentDetail = request.HoldComment
                  };
                  holdLotReqJson = Newtonsoft.Json.JsonConvert.SerializeObject(holdLotReq);
              }
          }
      }
  }
  """;
}

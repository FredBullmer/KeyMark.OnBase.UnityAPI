using System.Activities.Presentation.Metadata;
using System.ComponentModel;
using System.ComponentModel.Design;
using KeyMark.OnBase.UnityAPI.Activities.Design.Designers;
using KeyMark.OnBase.UnityAPI.Activities.Design.Properties;

namespace KeyMark.OnBase.UnityAPI.Activities.Design
{
    public class DesignerMetadata : IRegisterMetadata
    {
        public void Register()
        {
            var builder = new AttributeTableBuilder();
            builder.ValidateTable();

            var categoryAttribute = new CategoryAttribute($"{Resources.Category}");

            builder.AddCustomAttributes(typeof(UseOnBaseUnity), categoryAttribute);
            builder.AddCustomAttributes(typeof(UseOnBaseUnity), new DesignerAttribute(typeof(UseOnBaseUnityDesigner)));
            builder.AddCustomAttributes(typeof(UseOnBaseUnity), new HelpKeywordAttribute(""));

            builder.AddCustomAttributes(typeof(GetLifeCycles), categoryAttribute);
            builder.AddCustomAttributes(typeof(GetLifeCycles), new DesignerAttribute(typeof(GetLifeCyclesDesigner)));
            builder.AddCustomAttributes(typeof(GetLifeCycles), new HelpKeywordAttribute(""));


            MetadataStore.AddAttributeTable(builder.CreateTable());
        }
    }
}

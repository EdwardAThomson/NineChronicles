using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nekoyume.Game.Mail;
using Nekoyume.JsonConvertibles;
using UnityEngine;

namespace Nekoyume.State.Modifiers
{
    [Serializable]
    public class AvatarAttachmentMailNewSetter : AvatarStateModifier
    {
        [SerializeField]
        private List<JsonConvertibleGuid> guidList;

        public override bool IsEmpty => !guidList.Any();

        public AvatarAttachmentMailNewSetter(params Guid[] guidParams)
        {
            guidList = new List<JsonConvertibleGuid>();
            foreach (var guid in guidParams)
            {
                guidList.Add(new JsonConvertibleGuid(guid));
            }
        }

        public override void Add(IStateModifier<AvatarState> modifier)
        {
            if (!(modifier is AvatarAttachmentMailNewSetter m))
                return;

            foreach (var incoming in m.guidList.Where(incoming =>
                !guidList.Contains(incoming)))
            {
                guidList.Add(incoming);
            }
        }

        public override void Remove(IStateModifier<AvatarState> modifier)
        {
            if (!(modifier is AvatarAttachmentMailNewSetter m))
                return;

            foreach (var incoming in m.guidList.Where(incoming =>
                guidList.Contains(incoming)))   
            {
                guidList.Remove(incoming);
            }
        }

        public override AvatarState Modify(AvatarState state)
        {
            if (state is null)
                return null;

            var attachmentMails = state.mailBox.OfType<AttachmentMail>();
            foreach (var attachmentMail in attachmentMails)
            {
                foreach (var jsonConvertibleGuid in guidList)
                {
                    if (jsonConvertibleGuid.Value.Equals(attachmentMail.attachment.itemUsable.ItemId))
                    {
                        attachmentMail.New = true;
                    }
                    
                    // 지금은 false 처리를 안 해주고 있는데, 그 이유는 다른 곳에서 AttachmentMail을 사용하는 기존의 방법이 유지되어야 하기 때문임.
                    // 모든 로직을 수정한 후에는 false 처리도 해줘도 됨. 하지만, New 프로퍼티는 자산이 아니고 뷰에서만 사용하는 값이기 때문에 State에서
                    // 빠지는 것이 맞겠음.
                }
            }

            return state;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var guid in guidList)
            {
                sb.AppendLine(guid.Value.ToString());
            }

            return sb.ToString();
        }
    }
}
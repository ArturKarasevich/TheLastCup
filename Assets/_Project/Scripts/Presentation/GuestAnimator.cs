using System.Collections.Generic;
using UnityEngine;

namespace TheLastCup.Presentation
{
    public class GuestAnimator : MonoBehaviour
    {
        public static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
        public static readonly int IsSittingHash = Animator.StringToHash("IsSitting");
        public static readonly int IsTalkingHash = Animator.StringToHash("IsTalking");
        public static readonly int IsCollapsedHash = Animator.StringToHash("IsTwistCollapsed");
        public static readonly int MoodHash = Animator.StringToHash("Mood");

        public static readonly int DrinkTriggerHash = Animator.StringToHash("Drink");
        public static readonly int GoodReactionTriggerHash = Animator.StringToHash("ReactGood");
        public static readonly int BadReactionTriggerHash = Animator.StringToHash("ReactBad");
        public static readonly int CollapseTriggerHash = Animator.StringToHash("Collapse");
        public static readonly int LeaveTriggerHash = Animator.StringToHash("Leave");

        [SerializeField] private Animator _animator;

        private readonly Dictionary<int, AnimatorControllerParameterType> _paramTypes = new();
        private bool _savedParams;

        private void Awake()
        {
            FindAnimator();
        }

        public void FindAnimator()
        {
            if (!_animator)
                _animator = GetComponentInChildren<Animator>(true);

            SaveAnimatorParams();
        }

        private void SaveAnimatorParams()
        {
            if (_savedParams || !_animator || !_animator.runtimeAnimatorController)
                return;

            _paramTypes.Clear();
            foreach (var p in _animator.parameters)
                _paramTypes[p.nameHash] = p.type;

            _savedParams = true;
        }

        private bool HasParam(int hash, AnimatorControllerParameterType expectedType)
        {
            if (!_animator || !_animator.isActiveAndEnabled)
                return false;

            if (!_savedParams)
                SaveAnimatorParams();

            AnimatorControllerParameterType actualType;
            return _paramTypes.TryGetValue(hash, out actualType) && actualType == expectedType;
        }

        public void ResetAnimator()
        {
            SetWalking(false);
            SetSitting(false);
            SetTalking(false);
            SetCollapsed(false);
            SetMood(0);
        }

        public void SetWalking(bool walking)
        {
            if (HasParam(IsWalkingHash, AnimatorControllerParameterType.Bool))
                _animator.SetBool(IsWalkingHash, walking);
        }

        public void SetSitting(bool sitting)
        {
            if (HasParam(IsSittingHash, AnimatorControllerParameterType.Bool))
                _animator.SetBool(IsSittingHash, sitting);
        }

        public void SetTalking(bool talking)
        {
            if (HasParam(IsTalkingHash, AnimatorControllerParameterType.Bool))
                _animator.SetBool(IsTalkingHash, talking);
        }

        public void SetCollapsed(bool collapsed)
        {
            if (HasParam(IsCollapsedHash, AnimatorControllerParameterType.Bool))
                _animator.SetBool(IsCollapsedHash, collapsed);

            if (collapsed && HasParam(CollapseTriggerHash, AnimatorControllerParameterType.Trigger))
                _animator.SetTrigger(CollapseTriggerHash);
        }

        public void SetMood(int mood)
        {
            if (HasParam(MoodHash, AnimatorControllerParameterType.Int))
                _animator.SetInteger(MoodHash, mood);
        }

        public void PlayTeaReaction(int matchesCount)
        {
            if (HasParam(DrinkTriggerHash, AnimatorControllerParameterType.Trigger))
                _animator.SetTrigger(DrinkTriggerHash);

            if (matchesCount >= 3)
            {
                SetMood(1);
                if (HasParam(GoodReactionTriggerHash, AnimatorControllerParameterType.Trigger))
                    _animator.SetTrigger(GoodReactionTriggerHash);
            }
            else if (matchesCount <= 1)
            {
                SetMood(2);
                if (HasParam(BadReactionTriggerHash, AnimatorControllerParameterType.Trigger))
                    _animator.SetTrigger(BadReactionTriggerHash);
            }
            else
            {
                SetMood(3);
            }
        }

        public void PlayLeaveAnimation()
        {
            SetSitting(false);
            SetTalking(false);
            if (HasParam(LeaveTriggerHash, AnimatorControllerParameterType.Trigger))
                _animator.SetTrigger(LeaveTriggerHash);
        }
    }
}

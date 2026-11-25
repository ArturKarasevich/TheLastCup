using System;
using System.Collections.Generic;
using TheLastCup.Data;
using TheLastCup.Domain;
using UnityEngine;

namespace TheLastCup.Core
{
    public enum EndingRank
    {
        GoldenDawn,
        PaleDawn,
        FrozenNight
    }

    public class GuestEndingItem
    {
        public string GuestName;
        public int TeaScore;
        public string ResultTitle;
    }

    public class SessionSummary
    {
        public EndingRank Rank;
        public int TotalLight;
        public string Title;
        public string Description;
        public List<GuestEndingItem> GuestItems = new List<GuestEndingItem>();
    }

    public static class GameEvents
    {
        public static bool SkipMenuNextTime { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetAllEvents()
        {
            SkipMenuNextTime = false;
            OnCameraLockChanged = null;
            OnUIModeChanged = null;
            OnCameraFocusRequested = null;
            OnCameraFreeLookRequested = null;
            OnCameraReturnAndLookRequested = null;
            OnCameraLookRequested = null;
            OnFadeMoveRequested = null;
            OnFadeReturnRequested = null;
            OnSnapCameraRequested = null;
            OnSnapCameraBackRequested = null;
            OnEnterTeaStationRequested = null;
            OnHerbPicked = null;
            OnHerbAddRequested = null;
            OnTeaSlotChanged = null;
            OnAllHerbsAdded = null;
            OnTeapotSpawned = null;
            OnTeapotGrabbed = null;
            OnTuningForkRequested = null;
            OnTuningForkSubmitted = null;
            OnTeapotDropped = null;
            OnTeapotPlacedUnderBoiler = null;
            OnHotWaterRequested = null;
            OnTeaBrewed = null;
            OnTeapotServed = null;
            OnTeaSubmitted = null;
            OnTeapotBroken = null;
            OnResetTeaCycle = null;
            OnMagnifierRequested = null;
            OnGuestArrived = null;
            OnGuestDoorOpened = null;
            OnGuestAtCounter = null;
            OnIntroTalkEnded = null;
            OnTeaChecked = null;
            OnGuestSatAtTable = null;
            OnTableTalkStarted = null;
            OnGuestResultReached = null;
            OnItemUnlocked = null;
            OnHerbSecretUnlocked = null;
            OnLightChanged = null;
            OnStoryTwistStarted = null;
            OnGuestDone = null;
            OnGuestExitDoorRequested = null;
            OnGuestExitDoorOpened = null;
            OnGuestLeft = null;
            OnWhistleRetryRequested = null;
            OnSessionEnded = null;
            OnDemoEnded = null;
            OnShowSubtitle = null;
            OnHintPulseChanged = null;
            OnBookFocusChanged = null;
            OnBallFocusChanged = null;
            OnBallWordShown = null;
            OnBallInspected = null;
            OnFirstTeaStationEntered = null;
            OnTeaDumped = null;
            OnStartGameRequested = null;
            OnDebugSnapGuest = null;
            OnDebugSkipTalk = null;
        }
        public static event Action<bool> OnCameraLockChanged;
        public static event Action<bool> OnUIModeChanged;
        public static event Action<Transform> OnCameraFocusRequested;
        public static event Action OnCameraFreeLookRequested;
        public static event Action<Transform> OnCameraReturnAndLookRequested;
        public static event Action<Transform> OnCameraLookRequested;
        public static event Action<Transform, Action> OnFadeMoveRequested;
        public static event Action OnFadeReturnRequested;
        public static event Action<Transform> OnSnapCameraRequested;
        public static event Action OnSnapCameraBackRequested;
        public static event Action OnEnterTeaStationRequested;
        public static event Action<HerbInfo> OnHerbPicked;
        public static event Action<HerbInfo> OnHerbAddRequested;
        public static event Action<int, HerbInfo> OnTeaSlotChanged;
        public static event Action<Transform> OnAllHerbsAdded;

        public static event Action<Transform> OnTeapotSpawned;
        public static event Action<Transform> OnTeapotGrabbed;
        public static event Action OnTuningForkRequested;
        public static event Action<TeaMaker> OnTuningForkSubmitted;
        public static event Action OnTeapotDropped;
        public static event Action<Transform> OnTeapotPlacedUnderBoiler;
        public static event Action OnHotWaterRequested;
        public static event Action OnTeaBrewed;
        public static event Action<Transform> OnTeapotServed;
        public static event Action<TeaMaker> OnTeaSubmitted;
        public static event Action OnTeapotBroken;
        public static event Action OnResetTeaCycle;
        public static event Action<string> OnMagnifierRequested;
        public static event Action<GuestInfo> OnGuestArrived;
        public static event Action OnGuestDoorOpened;
        public static event Action<GuestInfo> OnGuestAtCounter;
        public static event Action<GuestInfo> OnIntroTalkEnded;
        public static event Action<TeaResult, GuestInfo> OnTeaChecked;
        public static event Action<string, GameObject> OnGuestSatAtTable;
        public static event Action<TeaResult, GuestInfo> OnTableTalkStarted;
        public static event Action<string, TalkOutcomeData> OnGuestResultReached;
        public static event Action<string> OnItemUnlocked;
        public static event Action<string> OnHerbSecretUnlocked;
        public static event Action<int> OnLightChanged;
        public static event Action OnStoryTwistStarted;
        public static event Action OnGuestDone;
        public static event Action OnGuestExitDoorRequested;
        public static event Action OnGuestExitDoorOpened;
        public static event Action OnGuestLeft;
        public static event Action OnWhistleRetryRequested;
        public static event Action<SessionSummary> OnSessionEnded;
        public static event Action OnDemoEnded;
        public static event Action<string> OnShowSubtitle;
        public static event Action<bool> OnHintPulseChanged;
        public static event Action<bool> OnBookFocusChanged;
        public static event Action<bool> OnBallFocusChanged;
        public static event Action<string> OnBallWordShown;
        public static event Action OnBallInspected;
        public static event Action OnFirstTeaStationEntered;
        public static event Action OnTeaDumped;
        public static event Action OnStartGameRequested;
        public static event Action OnDebugSnapGuest;
        public static event Action OnDebugSkipTalk;
        public static void SetCameraLocked(bool isLocked)
        {
            if (OnCameraLockChanged != null) OnCameraLockChanged.Invoke(isLocked);
        }
        public static void SetUIMode(bool isActive)
        {
            if (OnUIModeChanged != null) OnUIModeChanged.Invoke(isActive);
        }
        public static void RequestCameraFocus(Transform target)
        {
            if (OnCameraFocusRequested != null) OnCameraFocusRequested.Invoke(target);
        }
        public static void RequestCameraFreeLook()
        {
            if (OnCameraFreeLookRequested != null) OnCameraFreeLookRequested.Invoke();
        }
        public static void RequestCameraReturnAndLook(Transform target)
        {
            if (OnCameraReturnAndLookRequested != null) OnCameraReturnAndLookRequested.Invoke(target);
        }
        public static void RequestCameraLook(Transform target)
        {
            if (OnCameraLookRequested != null) OnCameraLookRequested.Invoke(target);
        }
        public static void RequestFadeMove(Transform target, Action onComplete = null)
        {
            if (OnFadeMoveRequested != null)
            {
                OnFadeMoveRequested.Invoke(target, onComplete);
            }
            else
            {
                if (OnSnapCameraRequested != null) OnSnapCameraRequested.Invoke(target);
                if (onComplete != null) onComplete.Invoke();
            }
        }

        public static void RequestFadeReturn()
        {
            if (OnFadeReturnRequested != null)
                OnFadeReturnRequested.Invoke();
            else if (OnSnapCameraBackRequested != null)
                OnSnapCameraBackRequested.Invoke();
        }
        public static void SnapCamera(Transform target)
        {
            if (OnSnapCameraRequested != null) OnSnapCameraRequested.Invoke(target);
        }
        public static void SnapCameraBack()
        {
            if (OnSnapCameraBackRequested != null) OnSnapCameraBackRequested.Invoke();
        }
        public static void RequestEnterTeaStation()
        {
            if (OnEnterTeaStationRequested != null) OnEnterTeaStationRequested.Invoke();
        }
        public static void PickHerb(HerbInfo herb)
        {
            if (OnHerbPicked != null) OnHerbPicked.Invoke(herb);
        }
        public static void RequestAddHerb(HerbInfo herb)
        {
            if (OnHerbAddRequested != null) OnHerbAddRequested.Invoke(herb);
        }
        public static void SetTeaSlot(int slot, HerbInfo herb)
        {
            if (OnTeaSlotChanged != null) OnTeaSlotChanged.Invoke(slot, herb);
        }
        public static void SendAllHerbsAdded(Transform teapotTransform)
        {
            if (OnAllHerbsAdded != null) OnAllHerbsAdded.Invoke(teapotTransform);
        }

        public static void SendTeapotSpawned(Transform teapotTransform)
        {
            if (OnTeapotSpawned != null) OnTeapotSpawned.Invoke(teapotTransform);
        }
        public static void GrabTeapot(Transform teapotTransform)
        {
            if (OnTeapotGrabbed != null) OnTeapotGrabbed.Invoke(teapotTransform);
        }
        public static void RequestTuningFork()
        {
            if (OnTuningForkRequested != null) OnTuningForkRequested.Invoke();
        }
        public static void SubmitTuningFork(TeaMaker service)
        {
            if (OnTuningForkSubmitted != null) OnTuningForkSubmitted.Invoke(service);
        }
        public static void DropTeapot()
        {
            if (OnTeapotDropped != null) OnTeapotDropped.Invoke();
        }
        public static void PlaceTeapotUnderBoiler(Transform slotPoint)
        {
            if (OnTeapotPlacedUnderBoiler != null) OnTeapotPlacedUnderBoiler.Invoke(slotPoint);
        }
        public static void RequestHotWater()
        {
            if (OnHotWaterRequested != null) OnHotWaterRequested.Invoke();
        }
        public static void BrewTea()
        {
            if (OnTeaBrewed != null) OnTeaBrewed.Invoke();
        }
        public static void ServeTeapot(Transform servePoint)
        {
            if (OnTeapotServed != null) OnTeapotServed.Invoke(servePoint);
        }
        public static void SubmitTea(TeaMaker service)
        {
            if (OnTeaSubmitted != null) OnTeaSubmitted.Invoke(service);
        }
        public static void SendTeapotBroken()
        {
            if (OnTeapotBroken != null) OnTeapotBroken.Invoke();
        }
        public static void ResetTeaCycle()
        {
            if (OnResetTeaCycle != null) OnResetTeaCycle.Invoke();
        }
        public static void RequestMagnifier(string herbId)
        {
            if (OnMagnifierRequested != null) OnMagnifierRequested.Invoke(herbId);
        }
        public static void SendGuestArrived(GuestInfo spirit)
        {
            if (OnGuestArrived != null) OnGuestArrived.Invoke(spirit);
        }
        public static void SendGuestDoorOpened()
        {
            if (OnGuestDoorOpened != null) OnGuestDoorOpened.Invoke();
        }
        public static void SendGuestAtCounter(GuestInfo spirit)
        {
            if (OnGuestAtCounter != null) OnGuestAtCounter.Invoke(spirit);
        }
        public static void SendIntroTalkEnded(GuestInfo spirit)
        {
            if (OnIntroTalkEnded != null) OnIntroTalkEnded.Invoke(spirit);
        }
        public static void SendTeaChecked(TeaResult result, GuestInfo spirit)
        {
            if (OnTeaChecked != null) OnTeaChecked.Invoke(result, spirit);
        }
        public static void SendGuestSatAtTable(string spiritId, GameObject modelRoot)
        {
            if (OnGuestSatAtTable != null) OnGuestSatAtTable.Invoke(spiritId, modelRoot);
        }
        public static void SendTableTalkStarted(TeaResult result, GuestInfo spirit)
        {
            if (OnTableTalkStarted != null) OnTableTalkStarted.Invoke(result, spirit);
        }
        public static void SendGuestResultReached(string spiritId, TalkOutcomeData outcome)
        {
            if (OnGuestResultReached != null) OnGuestResultReached.Invoke(spiritId, outcome);
        }
        public static void UnlockItem(string artifactName)
        {
            if (OnItemUnlocked != null) OnItemUnlocked.Invoke(artifactName);
        }
        public static void UnlockHerbSecret(string herbId)
        {
            if (OnHerbSecretUnlocked != null) OnHerbSecretUnlocked.Invoke(herbId);
        }
        public static void SendLightChanged(int totalLight)
        {
            if (OnLightChanged != null) OnLightChanged.Invoke(totalLight);
        }
        public static void StartStoryTwist()
        {
            if (OnStoryTwistStarted != null) OnStoryTwistStarted.Invoke();
        }
        public static void CompleteGuest()
        {
            if (OnGuestDone != null) OnGuestDone.Invoke();
        }
        public static void RequestGuestExitDoor()
        {
            if (OnGuestExitDoorRequested != null) OnGuestExitDoorRequested.Invoke();
        }
        public static void SendGuestExitDoorOpened()
        {
            if (OnGuestExitDoorOpened != null) OnGuestExitDoorOpened.Invoke();
        }
        public static void SendGuestLeft()
        {
            if (OnGuestLeft != null) OnGuestLeft.Invoke();
        }
        public static void RequestWhistleRetry()
        {
            if (OnWhistleRetryRequested != null) OnWhistleRetryRequested.Invoke();
        }
        public static void SendSessionEnded(SessionSummary summary)
        {
            if (OnSessionEnded != null) OnSessionEnded.Invoke(summary);
        }
        public static void FinishDemo()
        {
            if (OnDemoEnded != null) OnDemoEnded.Invoke();
        }
        public static void ShowSubtitle(string message)
        {
            if (OnShowSubtitle != null) OnShowSubtitle.Invoke(message);
        }
        public static void SetHintPulse(bool enabled)
        {
            if (OnHintPulseChanged != null) OnHintPulseChanged.Invoke(enabled);
        }
        public static void SendBookFocusChanged(bool isOpen)
        {
            if (OnBookFocusChanged != null) OnBookFocusChanged.Invoke(isOpen);
        }
        public static void SendBallFocusChanged(bool isOpen)
        {
            if (OnBallFocusChanged != null) OnBallFocusChanged.Invoke(isOpen);
        }
        public static void ShowBallWord(string keyword)
        {
            if (OnBallWordShown != null) OnBallWordShown.Invoke(keyword);
        }
        public static void SendBallInspected()
        {
            if (OnBallInspected != null) OnBallInspected.Invoke();
        }
        public static void SendFirstTeaStationEntered()
        {
            if (OnFirstTeaStationEntered != null) OnFirstTeaStationEntered.Invoke();
        }
        public static void SendTeaDumped()
        {
            if (OnTeaDumped != null) OnTeaDumped.Invoke();
        }
        public static void RequestStartGame()
        {
            if (OnStartGameRequested != null) OnStartGameRequested.Invoke();
        }
        public static void DebugSnapGuest()
        {
            if (OnDebugSnapGuest != null) OnDebugSnapGuest.Invoke();
        }
        public static void DebugSkipTalk()
        {
            if (OnDebugSkipTalk != null) OnDebugSkipTalk.Invoke();
        }
    }
}

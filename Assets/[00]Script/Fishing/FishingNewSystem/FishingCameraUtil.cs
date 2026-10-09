using UnityEngine;
using Unity.Cinemachine;

namespace FishingNewSystem
{
    // รวม logic หมุน FollowOffset ตาม yaw ผู้เล่นเอง ไม่พึ่ง Cinemachine BindingMode (ไม่ชัวร์ว่าทำงานตรงตามคาดจริง)
    // ใช้ร่วมกันทั้ง hookCamera (FishingCastController) และ reelCamera (FishingReelMinigame)
    internal static class FishingCameraUtil
    {
        public static void ForceWorldSpaceBinding(CinemachineFollow follow)
        {
            if (follow == null) return;
            var settings = follow.TrackerSettings;
            settings.BindingMode = Unity.Cinemachine.TargetTracking.BindingMode.WorldSpace;
            follow.TrackerSettings = settings;
        }

        public static Quaternion GetFlatYaw(Transform player)
        {
            Vector3 forwardFlat = Vector3.ProjectOnPlane(player.forward, Vector3.up);
            return forwardFlat.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(forwardFlat) : player.rotation;
        }

        public static void ApplyOffset(CinemachineFollow follow, Quaternion playerYaw, Vector3 localOffset)
        {
            if (follow == null) return;
            follow.FollowOffset = playerYaw * localOffset;
        }
    }
}

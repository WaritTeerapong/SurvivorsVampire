using UnityEngine;
public class PlayerDiedState : IPlayerState
{
    public void OnEnter(Player player)
    {
        // แปลงร่างเป็นผี (เปลี่ยนรูป, ปิดยิง, ให้ศัตรูเลิกตาม)
        player.BecomeGhostRpc();
    }

    public void OnUpdate(Player player) { }

    public void OnFixedUpdate(Player player)
    {
        // ผียังสามารถเดินบังคับได้ด้วยความเร็วเท่าเดิม!
        player.Movement.Move(player.InputHandler.MoveInput, player.Stats.CurrentStats.Value.MoveSpeed);
    }

    public void OnExit(Player player)
    {
        // เว้นไว้ทำระบบเกิดใหม่ (เช่น กลับไปใช้ภาพเดิม, เปิด Animator, ใส่ชื่อกลับเข้าเป้าหมายศัตรู)
    }
}
using UnityEngine;

public class DroneStrikerAI : DroneAIBrain
{
    [Header("Striker Setup")]
    public GateTarget targetGate;          // Ворота, через которые нужно пролететь

    [Tooltip("Смещение по высоте относительно центра ворот.")]
    public float heightOffset = 0f;

    [Tooltip("Насколько далеко за воротами дрон целится, чтобы точно пролететь насквозь.")]
    public float overshootDistance = 5f;

    [Tooltip("Если ближе этой дистанции к целевой точке — считаем, что цель достигнута.")]
    public float stopDistance = 1.5f;

    protected override Vector3 ComputeDesiredInput()
    {
        if (targetGate == null)
            return Vector3.zero;

        // Центр ворот
        Vector3 gateCenter = targetGate.GetTargetPosition();

        // Направление "вылета" из ворот — считаем, что forward гейта показывает "наружу"
        Vector3 gateForward = targetGate.transform.forward;
        if (gateForward.sqrMagnitude < 0.001f)
            gateForward = Vector3.forward;

        gateForward.Normalize();

        // Целевая точка — чуть ЗА воротами по направлению forward, чтобы дрон их ПРОЛЕТАЛ
        Vector3 desiredPos = gateCenter + gateForward * overshootDistance;
        desiredPos.y += heightOffset;

        Vector3 toTarget = desiredPos - transform.position;
        float dist = toTarget.magnitude;

        if (dist < stopDistance)
        {
            // Уже у цели / пролетел — можно остановиться
            return Vector3.zero;
        }

        Vector3 dirWorld = toTarget / dist;
        Vector3 dirLocal = transform.InverseTransformDirection(dirWorld);

        // x = влево/вправо (A/D), y = вперёд/назад (W/S), z = вверх/вниз (Space/Ctrl)
        float forwardInput = Mathf.Clamp(dirLocal.z, -1f, 1f);
        float strafeInput = Mathf.Clamp(dirLocal.x, -1f, 1f);
        float verticalInput = Mathf.Clamp(dirLocal.y, -1f, 1f);

        return new Vector3(strafeInput, forwardInput, verticalInput);
    }
}

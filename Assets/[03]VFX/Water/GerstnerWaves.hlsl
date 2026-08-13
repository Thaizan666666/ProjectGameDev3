// GerstnerWaves.hlsl
// ใช้เป็น "Custom Function Node" (Type: File) ใน Shader Graph
// อ่านค่าคลื่นจาก global properties ที่ WaterManager.cs ตั้งไว้ (waveData / _WaveCount)
// เพื่อให้ค่าคลื่นบน Shader (GPU) ตรงกับที่ C# script คำนวณ (CPU) แบบ 100%

// ต้องประกาศ global เดียวกับที่ WaterManager.cs ใช้ SetGlobalVectorArray("waveData", ...) ไว้
// waveData[0..9]  = (amplitude, direction(deg), wavelength, onmiDir)  ต่อคลื่นแต่ละลูก
// waveData[10..19] = (origin.x, origin.y, 0, 0)  จุดกำเนิดคลื่น (สำหรับ omni-directional wave)
float4 waveData[20];
int _WaveCount;

// ฟังก์ชันคำนวณคลื่นลูกเดียว คืนค่า offset (การขยับตำแหน่ง), normal สะสม
// และ jacobianSum (xx, xz, zz) สำหรับคำนวณ foam ที่ยอดคลื่น
void GerstnerWave(float4 wave, float2 origin, bool omni, float3 worldPos, float time,
                   inout float3 offset, inout float3 normalSum, inout float3 jacobianSum)
{
    float amplitude = wave.x;
    float directionDeg = wave.y;
    float wavelength = wave.z;

    // ป้องกันหารด้วยศูนย์
    wavelength = max(wavelength, 0.001);

    float w = 6.28318 / wavelength;      // wave number (2*PI / wavelength)
    float speed = sqrt(9.8 * w);         // ความเร็วคลื่นตามฟิสิกส์น้ำลึก (dispersion relation)
    float steepness = amplitude * w;     // ความชันของคลื่น (Q factor แบบง่าย)

    float2 dir;
    if (omni)
    {
        // คลื่นวงกลมจากจุดกำเนิด (ใช้กับ boat wake หรือ ripple)
        float2 toPoint = worldPos.xz - origin;
        dir = normalize(toPoint + 1e-6);
    }
    else
    {
        float rad = radians(directionDeg);
        dir = float2(cos(rad), sin(rad));
    }

    float phase = dot(dir, worldPos.xz) * w + time * speed;
    float sinP = sin(phase);
    float cosP = cos(phase);

    // Gerstner displacement: แนวนอนขยับตามทิศคลื่น, แนวตั้งคือความสูงคลื่น
    offset.x += steepness * amplitude * dir.x * cosP;
    offset.z += steepness * amplitude * dir.y * cosP;
    offset.y += amplitude * sinP;

    // สะสมค่าสำหรับคำนวณ normal (ความชันของผิวน้ำ ใช้ทำแสงเงา/reflection)
    normalSum.x += -dir.x * w * amplitude * cosP;
    normalSum.z += -dir.y * w * amplitude * cosP;
    normalSum.y += 1.0 - steepness * w * amplitude * sinP;

    // Jacobian ของ horizontal displacement (อนุพันธ์ของ offset.xz เทียบกับตำแหน่งเดิม x,z)
    // ใช้ตรวจจุดที่คลื่นเริ่ม "พับ/ม้วน" (crest) ซึ่งเป็นตำแหน่งที่ควรเกิด foam
    // k คือตัวคูณร่วมของอนุพันธ์ทั้งสามพจน์ (มาจากการ diff offset.x, offset.z เทียบ x,z)
    float k = steepness * amplitude * w * sinP;
    jacobianSum.x += -k * dir.x * dir.x; // d(offset.x)/dx
    jacobianSum.y += -k * dir.x * dir.y; // d(offset.x)/dz == d(offset.z)/dx
    jacobianSum.z += -k * dir.y * dir.y; // d(offset.z)/dz
}

// ฟังก์ชันหลักที่เรียกใน Shader Graph Custom Function Node
// Inputs:  WorldPos (float3), TimeValue (float)
// Outputs: Offset (float3)     - เอาไปบวกเข้ากับ Position node
//          WaveNormal (float3) - เอาไปต่อ Normal ของ material
//          Foam (float, 0..1)  - มาก = ยอดคลื่นกำลังม้วน/แตก เอาไป Lerp กับสี foam ใน Shader Graph
void GerstnerWaves_float(float3 WorldPos, float TimeValue, out float3 Offset, out float3 WaveNormal, out float Foam)
{
    Offset = float3(0, 0, 0);
    float3 normalSum = float3(0, 0, 0);
    float3 jacobianSum = float3(0, 0, 0); // (dDxDx, dDxDz, dDzDz)

    int count = min(_WaveCount, 10);
    for (int i = 0; i < count; i++)
    {
        float4 wave = waveData[i];
        float2 origin = waveData[i + 10].xy;
        bool omni = wave.w > 0.5;
        GerstnerWave(wave, origin, omni, WorldPos, TimeValue, Offset, normalSum, jacobianSum);
    }

    WaveNormal = normalize(normalSum);

    // Jacobian determinant: พื้นราบ = 1, ยิ่งเข้าใกล้ 0 หรือติดลบ = คลื่นกำลังม้วนตัว/แตกเป็นฟอง
    float jacobian = (1.0 + jacobianSum.x) * (1.0 + jacobianSum.z) - jacobianSum.y * jacobianSum.y;
    Foam = saturate(1.0 - jacobian);
}

// ---- ฝั่ง C# (buoyancy + foam) ----
// สูตรเดียวกันนี้ถูกเขียนซ้ำใน GerstnerWaveMath (Assets/[00]Script/Water/BoatBuoyancy.cs):
// - SampleWaves()  ตรงกับ Offset/WaveNormal ด้านบน (ใช้กับแรงลอยตัว/เอียงเรือ)
// - SampleFoam()   ตรงกับ Foam ด้านบน (ใช้กับ BoatWakeFoam สำหรับ wake ที่หัวเรือ)

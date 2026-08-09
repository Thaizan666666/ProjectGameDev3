# Water + Boat Buoyancy — Setup Guide (ทำใน Unity Editor)

เอกสารนี้คือขั้นตอนที่เหลือที่ต้องทำ**ในตัว Unity Editor เอง** (คลิก/ลาก/วาง) หลังจากที่โค้ดฝั่ง C# และ HLSL ถูกวางไว้ในโปรเจกต์แล้ว ดูรายละเอียดการตัดสินใจ/สถาปัตยกรรมทั้งหมดได้ที่ `Docs/water-system-brief.md`

ไฟล์ที่มีอยู่แล้วในโปรเจกต์ (ไม่ต้องสร้างใหม่):
- `Assets/[00]Script/Water/WaterManager.cs`
- `Assets/[00]Script/Water/BoatBuoyancy.cs`
- `Assets/[00]Script/Water/BoatWakeFoam.cs`
- `Assets/[03]VFX/Water/GerstnerWaves.hlsl`
- `Assets/[03]VFX/Water.shadergraph` (มีอยู่ก่อนแล้ว — มี shallow/deep water coloring แต่ยังไม่มี wave displacement)

---

## ขั้นที่ 1 — ต่อ Custom Function Node เข้า Shader Graph

### ตัวแปรที่ต้องประกาศก่อน (ยังไม่ต้องต่อสายอะไรในขั้นนี้)

**A. Global variables ฝั่ง HLSL (มีอยู่แล้วในไฟล์ ไม่ต้องทำอะไรเพิ่ม)**
ประกาศไว้บนสุดของ `GerstnerWaves.hlsl` แล้ว และถูกเซ็ตค่าจาก `WaterManager.cs` ผ่าน `Shader.SetGlobalVectorArray` / `Shader.SetGlobalInt` ทุกเฟรม — งานของเราคือแค่รู้ว่ามันมีอยู่ ไม่ต้องไปสร้างใน Shader Graph:
| ชื่อตัวแปร | Type | ใครเป็นคนตั้งค่า |
|---|---|---|
| `waveData` | `float4[20]` | `WaterManager.PushToGpu()` |
| `_WaveCount` | `int` | `WaterManager.PushToGpu()` |

**B. Input/Output ของ Custom Function Node (ต้องสร้างเอง ก่อนต่อสาย)**
เพิ่มผ่าน Graph Inspector (คลิก `+`) — ชื่อ/Type ต้องตรงกับ signature ของฟังก์ชัน `GerstnerWaves_float` เป๊ะ ๆ ไม่งั้น node จะ error:

| ทิศทาง | ชื่อ | Type | ค่า/ที่มา |
|---|---|---|---|
| Input | `WorldPos` | Vector3 | มาจาก Position node (Space = World) |
| Input | `TimeValue` | Float | มาจาก Time node (output `Time`) |
| Output | `Offset` | Vector3 | เอาไปบวกกับ Position เดิมของ mesh |
| Output | `WaveNormal` | Vector3 | เอาไปต่อ Normal ของ Fragment block |
| Output | `Foam` | Float | เอาไปเป็น T ของ Lerp สี foam |

---

### วิธีต่อสาย (หลังประกาศตัวแปรด้านบนครบแล้ว)

1. เปิด `Assets/[03]VFX/Water.shadergraph` (ดับเบิลคลิก)
2. คลิกขวาบนพื้นที่ว่าง → **Create Node** → หา **Custom Function**
3. เลือก node ที่สร้างขึ้นมา แล้วตั้งค่าใน Graph Inspector (ขวามือ):
   - **Type**: `File`
   - **Source**: ลากไฟล์ `Assets/[03]VFX/Water/GerstnerWaves.hlsl` มาใส่
   - **Name**: พิมพ์ `GerstnerWaves_float` (ต้องตรงกับชื่อฟังก์ชันเป๊ะ ๆ)
4. เพิ่ม Input/Output ตามตาราง B ด้านบน (คลิก `+` ใน Inspector ทีละช่อง)
5. ต่อสาย (wire):
   - **Position node** (Space = World) → `WorldPos`
   - **Time node** (ใช้ output `Time`) → `TimeValue`
   - `Offset` → บวก (Add node) เข้ากับ **Position node** เดิมของ mesh → ต่อเข้า **Position** ของ Master Stack (ช่อง Vertex)
   - `WaveNormal` → ต่อเข้า **Normal** ของ Master Stack (ช่อง Fragment) — ถ้ามี normal map อยู่แล้วให้ blend/normalize รวมกัน อย่าต่อทับตรง ๆ
   - `Foam` → ใช้เป็น T ของ **Lerp** ระหว่างสีน้ำปกติ (shallow/deep water ที่มีอยู่แล้ว) กับสี foam (ขาว/ฟ้าอ่อน)
     - แนะนำใส่ **Saturate** หรือ **Remap** (เช่น remap 0.5–1 → 0–1) ก่อนเข้า Lerp เพื่อคุมว่า foam เริ่มขึ้นตอนคลื่นม้วนแค่ไหน ปรับค่าตามที่ดูสวยจริงในฉาก
6. กด **Save Asset** (มุมขวาบนของ Shader Graph window)

**เช็คก่อนไปขั้นต่อไป:** ยังไม่มี `WaterManager` ในฉาก ตอนนี้ `waveData[20]` จะเป็น 0 ทั้งหมด (Unity default ค่า global properties ที่ไม่เคย set = 0) เพราะฉะนั้นน้ำจะยังนิ่ง ไม่ขยับ — ให้ทำขั้นที่ 2 ก่อนถึงจะเห็นคลื่นจริง

---

## ขั้นที่ 2 — สร้าง WaterManager ในฉาก

1. คลิกขวาใน Hierarchy → **Create Empty** → ตั้งชื่อ `WaterManager`
2. **Add Component** → ค้นหา `WaterManager` (namespace `WaterSystem`) → เพิ่มเข้าไป
3. ใน Inspector:
   - `Base Waves` มีค่า default 3 ลูกให้แล้ว (ปรับ amplitude/direction/wavelength ได้ตามชอบ)
   - **ถ้ามี Wind Zone ในฉาก**: ลาก GameObject ที่มี component `Wind Zone` มาใส่ช่อง `Wind Zone` — จะทำให้แรง/ทิศทางลมควบคุม amplitude และ direction ของคลื่นอัตโนมัติ
   - **ถ้ายังไม่มี Wind Zone**: `GameObject > 3D Object > Wind Zone` สร้างใหม่ได้ (โหมด Directional) แล้วค่อยลากมาใส่ทีหลังก็ได้ ไม่ใส่ก็ทำงานได้ปกติ (ใช้ `baseWaves` ตรง ๆ)

---

## ขั้นที่ 3 — ต่อ BoatBuoyancy เข้ากับเรือ

1. เลือก GameObject เรือที่มี `Rigidbody` อยู่แล้ว (หรือเพิ่ม Rigidbody ถ้ายังไม่มี)
2. **Add Component** → `BoatBuoyancy`
3. ใน Inspector:
   - `Water Manager` → ลาก GameObject `WaterManager` จากขั้นที่ 2 มาใส่ (สำคัญ — ถ้าไม่ใส่ต้องไปกรอก `waves` เองให้ตรงกับ `WaterManager` เป๊ะ ๆ ไม่งั้น CPU/GPU จะไม่ sync กัน)
   - `Float Points` → สร้าง child Transform เปล่า 4 จุดใต้เรือ (หัว/ท้าย/ซ้าย/ขวา) วางตำแหน่งให้อยู่ที่ขอบล่างของตัวเรือ แล้วลากทั้ง 4 มาใส่ array นี้
   - ปรับ `Buoyancy Force`, `Water Drag`, `Water Angular Drag` ตามน้ำหนัก/ขนาดเรือ (ค่า default พอใช้ได้กับเรือขนาดกลาง ลองรันแล้วค่อยจูน)

---

## ขั้นที่ 4 — ต่อ BoatWakeFoam ที่หัวเรือ

1. สร้าง child GameObject ใต้เรือ ตั้งชื่อ `BowFoam` วางตำแหน่งไว้ที่หัวเรือ ระดับผิวน้ำ
2. **Add Component** → `Particle System` (Unity จะสร้าง component มาให้พร้อมค่า default)
3. **Add Component** → `BoatWakeFoam`
4. ใน Inspector ของ `BoatWakeFoam`:
   - `Bow Point` → ลาก transform ของตัวมันเอง หรือ transform อื่นที่ตรงตำแหน่งหัวเรือจริง ๆ
   - `Boat Rigidbody` → ลาก Rigidbody ของเรือ (ตัวแม่)
   - `Water Manager` → ลาก `WaterManager` เดียวกับขั้นที่ 2-3
5. ปรับแต่ง Particle System เอง (สคริปต์คุมแค่ `Emission → Rate over Time`):
   - **Shape**: Cone หรือ Sphere เล็ก ๆ ที่หัวเรือ
   - **Renderer**: ใส่ material/texture โฟม-ขาว (billboard หรือ soft particle)
   - **Start Color**: ขาว/ฟ้าอ่อน, **Start Lifetime**: สั้น ๆ (~0.5–1.5s)
   - **Size over Lifetime**: ให้ค่อย ๆ ขยายแล้วจางหาย (ดู reference สไตล์ Sea of Thieves)
   - ปิด **Emission → Rate over Time** ไม่ต้องตั้งค่าคงที่ (สคริปต์จะ override ให้เองตอน Play)

---

## ขั้นที่ 5 — ทดสอบ

1. กด Play
2. เช็คว่าผิวน้ำใน Shader Graph ขยับเป็นคลื่นจริง (ถ้านิ่ง → เช็คว่า `WaterManager` อยู่ในฉากและ enable อยู่)
3. เช็คว่าเรือลอย/เอียงตามคลื่น (ถ้าเรือลอยนิ่งหรือทะลุน้ำ → เช็ค `Float Points` ว่าตำแหน่งเริ่มต้นใกล้ผิวน้ำพอหรือยัง)
4. ขับเรือให้เร็วขึ้น → ควรเห็น foam ที่หัวเรือหนาแน่นขึ้นตาม `BoatWakeFoam`
5. ถ้ามี Wind Zone → ลองปรับ `Wind Main`/`Wind Turbulence` ใน Inspector ตอน Play ดูว่าคลื่นเปลี่ยนตามจริงไหม

---

## งานที่ยังไม่ทำ (นอกเหนือจาก setup ข้างบน)

- ดึงซอร์ส `GerstnerWavesJobs` จาก Boat Attack repo (https://on.unity.com/3jeA8yg) มาเทียบ ถ้าจะ optimize ด้วย Job System + Burst ในอนาคต
- ทดสอบ workflow ย้าย URP → HDRP จริง (ต้องติดตั้ง HDRP package ก่อน โปรเจกต์นี้มีแค่ URP ตอนนี้)

# Unity Water + Boat Buoyancy Project Brief

โปรเจกต์: ทำน้ำด้วย Shader ให้เรือโยกตามคลื่นสมจริง สไตล์ Sea of Thieves
Engine: Unity, เริ่มที่ URP แต่วางแผนย้ายไป HDRP ในอนาคต — อ้างอิงระบบจาก Unity Boat Attack (WaterSystem namespace)

## ⚠️ การตัดสินใจสำคัญ: Render Pipeline

โปรเจกต์นี้จะย้ายจาก **URP → HDRP** ในอนาคต ดังนั้น:

- **ห้ามใช้ Stylized Water 3** (Staggart Creations) — รองรับเฉพาะ URP เท่านั้น ไม่มีทางอัปเกรดไป HDRP ได้ ต้องเขียนใหม่ทั้งหมดถ้าจะย้าย pipeline (เท่ากับสร้าง asset ใหม่)
- **แนวทางที่เลือกใช้: วิธีที่ 3 — เขียน Gerstner Wave เอง (DIY, Boat Attack-style)**
  - เหตุผล: สูตร Gerstner wave ที่เขียนเป็น HLSL Custom Function Node ใน Shader Graph ใช้ข้ามได้ทั้ง URP และ HDRP (ไม่ผูกกับ pipeline-specific API) ตอนย้าย pipeline แค่ปรับ Master Stack target ใหม่ ส่วนไฟล์ HLSL คำนวณคลื่นใช้ซ้ำได้เกือบ 100%
  - ทางเลือกสำรอง (ถ้าต้องการของสำเร็จรูปในอนาคต): Crest Ocean System รองรับทั้ง URP และ HDRP ในแพ็กเดียว

---

## เป้าหมาย

1. ทำผิวน้ำด้วย Shader (Gerstner Waves) ให้ดูเหมือนคลื่นทะเลจริง
2. ทำให้เรือ (Rigidbody) ลอย เอียง และโยกตามรูปทรงคลื่นจริง (ไม่ใช่แค่ขึ้น-ลงตรงๆ)
3. สไตล์ภาพอ้างอิง Sea of Thieves — คลื่นสวยงาม มี foam, การไล่สีตามความลึก
4. ใช้ Wind (Unity Wind Zone หรือ custom) เป็นตัวแปรควบคุมความแรง/ทิศทางคลื่น

---

## สถาปัตยกรรมที่ตกลงกันไว้

### หลักการสำคัญที่สุด: CPU/GPU ต้องใช้สูตรคลื่นเดียวกัน

- **GPU (Shader)** — ขยับ vertex ของ mesh น้ำที่มองเห็นบนจอ ด้วย Gerstner wave function ใน Shader Graph (ผ่าน Custom Function Node)
- **CPU (C#)** — คำนวณความสูง/ความเอียงของคลื่น ณ ตำแหน่งจุดลอยของเรือ ด้วยสูตร Gerstner **เดียวกัน** เพื่อสั่งแรง buoyancy
- ห้ามใช้ `AsyncGPUReadback` ดึงค่าจาก GPU มาใช้กับ buoyancy เพราะมี latency 1-3 เฟรมเสมอ (มีการ block/delay ในตัว API) ทำให้เรือลอยไม่ตรงจังหวะกับคลื่นที่เห็นจริงบนจอ โดยเฉพาะตอนคลื่นแรง/เรือเร็ว
- วิธีที่ถูกต้อง (ใช้ในโปรเจกต์ Unity Boat Attack จริง): เขียนฟังก์ชัน Gerstner wave **สองชุดที่สูตรตรงกัน** — ชุด HLSL สำหรับ shader และชุด C# (แนะนำใช้ Job System + Burst Compiler เพื่อความเร็ว) สำหรับฝั่ง physics/buoyancy

### อ้างอิงจาก Unity Boat Attack (โปรเจกต์ตัวอย่างทางการของ Unity)

- Repo: https://on.unity.com/3jeA8yg
- มีระบบน้ำ + คลื่น + buoyancy เรือ สำเร็จสมบูรณ์แล้ว โอเพนซอร์สให้ดูโค้ดจริงได้
- โครงสร้างสำคัญที่พบใน `Water.cs` (แนบไว้ในบทสนทนาแล้ว):
  - คลาส `Water : MonoBehaviour` เป็นตัวจัดการหลัก (ไม่ใช่ shader โดยตรง)
  - ตั้งค่าคลื่นแบบสุ่มใน `SetupWaves()` (amplitude, direction, wavelength ต่อคลื่นแต่ละลูก)
  - ส่งค่าคลื่นเข้า GPU ผ่าน global shader properties:
    - `Shader.SetGlobalVectorArray("waveData", ...)` — array ของ Vector4 (amplitude, direction, wavelength, omniDir) + origin
    - `Shader.SetGlobalInt("_WaveCount", ...)`
    - `Shader.SetGlobalBuffer("_WaveDataBuffer", waveBuffer)` — ใช้เมื่อ compute shader รองรับ
  - เรียก `GerstnerWavesJobs.UpdateHeights()` ใน `LateUpdate()` — นี่คือคลาสฝั่ง CPU (Job System) ที่คำนวณความสูงคลื่นสำหรับ buoyancy คู่กับฝั่ง GPU (**ยังไม่มีซอร์สโค้ดคลาสนี้ในมือ** — ควรไปดึงจาก repo Boat Attack มาดูจริง หรือให้ Claude Code ช่วยเขียนใหม่ตามสูตรที่มีอยู่แล้ว)
  - มี depth capture (`CaptureDepthMap`) สำหรับทำ foam ที่ชายฝั่งและปรับสีน้ำตามความลึก
  - รองรับ reflection 3 แบบ: Cubemap, Reflection Probe, Planar Reflection

### ไฟล์ที่สร้างไว้แล้ว: `GerstnerWaves.hlsl`

ไฟล์ HLSL สำหรับใช้เป็น Custom Function Node ใน Shader Graph โดย:
- อ่านค่าจาก global `waveData[20]` และ `_WaveCount` (ตัวแปรเดียวกับที่ `Water.cs` ตั้งไว้ ไม่ต้องประกาศใหม่ใน Shader Graph blackboard)
- ฟังก์ชันหลัก `GerstnerWaves_float(WorldPos, TimeValue, out Offset, out WaveNormal)`
- คำนวณ offset ตำแหน่ง vertex (แนวนอน+แนวตั้ง) และ normal สะสมจากคลื่นทุกลูก
- รองรับทั้งคลื่นทิศทางเดียว (directional) และคลื่นวงกลมจากจุดกำเนิด (omni-directional, เหมาะกับ boat wake)

**การ wire ใน Shader Graph:**
1. Custom Function Node → Type: File → เลือกไฟล์นี้ → Name: `GerstnerWaves_float`
2. Input `WorldPos` (Vector3) ← Position node (mode = World)
3. Input `TimeValue` (Float) ← Time node
4. Output `Offset` → บวกเข้า Position → Vertex Position ของ Master Stack
5. Output `WaveNormal` → Normal ของ Master Stack

**งานที่ยังไม่ได้ทำ:** เขียน C# counterpart (buoyancy script) ที่ใช้สูตรเดียวกันกับไฟล์ HLSL นี้ เพื่อคำนวณความสูงคลื่น ณ ตำแหน่งเรือ แล้วสั่ง `AddForceAtPosition` ตามหลัก Archimedes' principle

---

## เทคนิคเสริมสไตล์ Sea of Thieves

- Rare ใช้ Vertex Displacement โดย CPU คำนวณตำแหน่ง vertex แล้ว GPU ขยับ/re-position vertex เพื่อความลื่นไหลของคลื่น
- ใช้ Gerstner Waves (trochoidal wave) เป็นพื้นฐาน — อนุภาคน้ำเคลื่อนที่เป็นวงกลม ไม่ใช่แค่ sine ธรรมดา
- Foam/whitewater ที่หัวคลื่นและหัวเรือ — ทำด้วย Particle System หรือ shader แยกที่ blend ตาม wave steepness
- สี gradient น้ำไล่ตามความลึก (shallow → deep) — ใช้ depth texture ที่ `CaptureDepthMap()` ทำอยู่แล้วในสคริปต์
- Fresnel effect ให้น้ำสะท้อนแสงตามมุมมอง

---

## เรื่อง Wind

- Unity มี `Wind Zone` component (`GameObject > 3D Object > Wind Zone`) แต่ **ใช้ได้กับต้นไม้และ Particle System เท่านั้น** ไม่มีผลกับ mesh น้ำโดยตรง
- 2 โหมด: Directional (ทั่ว scene) / Spherical (รัศมีทรงกลม)
- วิธีเชื่อม Wind เข้ากับคลื่นน้ำ: ดึงค่า `windMain`, `windTurbulence`, ทิศทางจาก Wind Zone transform ผ่าน C# แล้วป้อนเป็น parameter เข้าสูตร Gerstner wave (แรงลม → amplitude, ทิศทาง → wave direction, turbulence → ความสุ่มของผิวน้ำ) ส่งเข้า shader ผ่าน `material.SetFloat(...)`

---

## Asset สำเร็จรูป (ทางเลือกถ้าไม่อยากเขียนเองทั้งหมด)

- **Crest Ocean System** — รองรับ FFT waves + Gerstner waves, มี foam, dynamic wave simulation, boat wake

## Reference / Tutorial links

- Habrador — Make a realistic boat in Unity with C#: https://www.habrador.com/tutorials/unity-boat-tutorial/
- YouTube — Buoyancy with Archimedes principle: https://www.youtube.com/watch?v=5W1nRb-fKn0
- Vertex Fragment — Buoyancy for Dummies: https://www.vertexfragment.com/ramblings/buoyancy-for-dummies/
- GitHub: dbrizov/Unity-WaterBuoyancy: https://github.com/dbrizov/Unity-WaterBuoyancy
- Unity Boat Attack repo: https://on.unity.com/3jeA8yg
- GitHub: bobboli/gerstner-water: https://github.com/bobboli/gerstner-water
- Alex Tardif — Water Walkthrough (Gerstner ผ่าน HLSL): https://alextardif.com/Water.html
- 80.lv — Augmented Gerstner Waves in Unreal (หลักการปรับใช้ได้กับ Unity): https://80.lv/articles/breakdown-setting-up-augmented-gerstner-waves-in-unreal-engine

---

## งานถัดไปที่ควรทำ (สำหรับ Claude Code)

1. [x] ตัดสินใจแนวทาง: DIY Gerstner wave (ไม่ใช้ Stylized Water 3 เพราะติด URP-only)
2. [x] เขียน C# Gerstner wave function ที่สูตรตรงกับ `GerstnerWaves.hlsl` — `Assets/[00]Script/Water/BoatBuoyancy.cs` (คลาส `GerstnerWaveMath`)
3. [x] เขียน Boat Buoyancy script: หาความสูงคลื่น ณ จุดลอย 4 จุดของเรือ (หัว/ท้าย/ซ้าย/ขวา) แล้ว `AddForceAtPosition` ตาม Archimedes' principle ใน `FixedUpdate` — `Assets/[00]Script/Water/BoatBuoyancy.cs` (คลาส `BoatBuoyancy`)
4. [ ] ดึงซอร์สโค้ด `GerstnerWavesJobs` จาก Boat Attack repo มาเทียบ/อ้างอิงเพิ่มเติม (ถ้าต้องการ optimize ด้วย Job System + Burst ภายหลัง) — ยังไม่ทำ ต้องดึงจาก repo ภายนอกจริง ไม่ใช่งานที่ทำแบบ offline ได้
5. [ ] ต่อ Custom Function Node (`GerstnerWaves.hlsl`) เข้า Shader Graph จริง (`Assets/[03]VFX/Water.shadergraph`) ตามขั้นตอนด้านบน — ทำแยกกันสำหรับ URP และ (ในอนาคต) HDRP target — **ต้องทำในหน้า Shader Graph editor ของ Unity เอง (manual, ไม่มีทางแก้ .shadergraph JSON แทนได้อย่างปลอดภัย)**
6. [x] เพิ่ม foam ที่ยอดคลื่น + ระบบ wake foam ที่หัวเรือ:
   - `GerstnerWaves_float` ใน `GerstnerWaves.hlsl` เพิ่ม output `Foam` (0..1) จาก Jacobian ของ horizontal displacement — เอาไป Lerp กับสี foam ใน Shader Graph ได้เลย (ดูขั้นตอน wiring ด้านล่าง)
   - `GerstnerWaveMath.SampleFoam()` ใน `BoatBuoyancy.cs` คือสูตรเดียวกันฝั่ง CPU
   - `Assets/[00]Script/Water/BoatWakeFoam.cs` — คุม Particle System ที่หัวเรือ ให้ emission rate ขึ้นกับความเร็วเรือ หรือค่า foam จากคลื่นธรรมชาติ (ยังต้องสร้าง/ตั้งค่า Particle System + texture เองใน Editor)
7. [x] เชื่อม Wind Zone เข้ากับ parameter ของคลื่น (amplitude/direction/turbulence) — `Assets/[00]Script/Water/WaterManager.cs` เป็น single source of truth ของ `Wave[]` ทั้งฉาก อ่านค่าจาก `WindZone` แล้ว push เข้า global shader properties (`waveData`, `_WaveCount`) พร้อมกับให้ `BoatBuoyancy`/`BoatWakeFoam` ดึงชุดคลื่นเดียวกันไปใช้ฝั่ง CPU
8. [ ] ทดสอบ workflow ย้าย URP → HDRP กับ Shader Graph ที่ใช้ `GerstnerWaves.hlsl` เพื่อยืนยันว่าใช้ซ้ำได้จริงตามที่วางแผนไว้ — ต้องมี HDRP package ติดตั้งจริงถึงจะทดสอบได้ ปัจจุบันโปรเจกต์มีแค่ URP

---

## ขั้นตอนที่เหลือใน Unity Editor (ทำเองตรงนี้)

1. เปิด `Assets/[03]VFX/Water.shadergraph`
2. เพิ่ม **Custom Function Node** → Type: File → เลือกไฟล์ `Assets/[03]VFX/Water/GerstnerWaves.hlsl` → Name: `GerstnerWaves_float`
   - Input `WorldPos` (Vector3) ← Position node (mode = World)
   - Input `TimeValue` (Float) ← Time node
   - Output `Offset` → บวกเข้า Position → Vertex Position ของ Master Stack
   - Output `WaveNormal` → Normal ของ Master Stack
   - Output `Foam` → Lerp ระหว่างสีน้ำ (ที่มีอยู่แล้ว: shallow/deep water) กับสี foam (ขาว/ฟ้าอ่อน) โดยใช้ `Foam` เป็น T ของ Lerp (แนะนำใส่ Remap/Saturate ก่อน เพื่อคุม threshold ที่ foam เริ่มขึ้น)
3. ในฉาก: สร้าง GameObject ว่างชื่อ `WaterManager` ใส่ component `WaterManager` (ตั้งค่า `baseWaves` หรือปล่อย default ก็ได้) — ถ้ามี Wind Zone ในฉากให้ลากมาใส่ช่อง `Wind Zone`
4. บนเรือ: component `BoatBuoyancy` ที่มีอยู่แล้ว → ลาก `WaterManager` เข้าช่อง `Water Manager` (แทนการตั้ง `waves` เอง) → ลาก floatPoints (หัว/ท้าย/ซ้าย/ขวา) ให้ครบ
5. ที่หัวเรือ: สร้าง child GameObject ใส่ `ParticleSystem` + component `BoatWakeFoam` → ตั้งค่า `bowPoint`, `boatRigidbody`, `waterManager` → ปรับ shape/texture/สีของ Particle System เอง (สคริปต์คุมแค่ emission rate)

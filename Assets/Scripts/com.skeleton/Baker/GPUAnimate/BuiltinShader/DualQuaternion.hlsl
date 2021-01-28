float3 rotationQ(float4 rotation, float3 positionR)
    {
      float num1 = rotation.x * 2;
      float num2 = rotation.y * 2;
      float num3 = rotation.z * 2;
      float num4 = rotation.x * num1;
      float num5 = rotation.y * num2;
      float num6 = rotation.z * num3;
      float num7 = rotation.x * num2;
      float num8 = rotation.x * num3;
      float num9 = rotation.y * num3;
      float num10 = rotation.w * num1;
      float num11 = rotation.w * num2;
      float num12 = rotation.w * num3;
      float3 vector3;
      vector3.x =  (1 - ( num5 +  num6)) *  positionR.x + ( num7 -  num12) *  positionR.y + ( num8 +  num11) *  positionR.z;
      vector3.y =  ( num7 +  num12) *  positionR.x + (1 - ( num4 +  num6)) *  positionR.y + ( num9 -  num10) *  positionR.z;
      vector3.z =( num8 -  num11) *  positionR.x + ( num9 +  num10) *  positionR.y + (1 - ( num4 +  num5)) *  positionR.z;
      return vector3;
    }

   
     float4 mulifyQ(float4 lhs, float4 rhs)
    {
        return float4((lhs.w * rhs.x +  lhs.x *  rhs.w +  lhs.y *  rhs.z -  lhs.z *  rhs.y), ( lhs.w *  rhs.y +  lhs.y *  rhs.w +  lhs.z *  rhs.x -  lhs.x *  rhs.z),  ( lhs.w *  rhs.z +  lhs.z *  rhs.w +  lhs.x *  rhs.y -  lhs.y *  rhs.x),  ( lhs.w *  rhs.w -  lhs.x *  rhs.x -  lhs.y *  rhs.y -  lhs.z *  rhs.z));
    }
     float3 Translation(float4 realQ, float4 dualQ)
    {
        float4 Conjugate = float4(-realQ.x, -realQ.y, -realQ.z, realQ.w);
        float4 TQ = mulifyQ(dualQ ,Conjugate);
        return float3( TQ.x + TQ.x,TQ.y + TQ.y,TQ.z + TQ.z);
    }
    
      float3 DQToPosition(float2x4 dualQ,float3 positionOS){
        return Translation(dualQ[0],dualQ[1])+rotationQ(dualQ[0],positionOS);
    }
    
    float2x4 normalizeDual(float2x4 dualQ){
        float4 real=dualQ[0];
        float4 dual=dualQ[1];
        float4 length2=dot(real,real);
        float4 length =sqrt(length2);
        real=real/length;
        dual=dual/length;  
        //res.Dual = res.Dual.Sub(res.Real.Scale(res.Real.Dot(res.Dual) * (length * length)));
        //dual=dual-(real* dot(real,dual)*length2);
        return float2x4(real,dual);
    }
    
    
    
struct dual_quaternion
{
	float4 rotation_quaternion;
	float4 translation_quaternion;
};

float4 QuaternionInvert(float4 q)
{
	q.xyz *= -1;
	return q;
}

float4 QuaternionMultiply(float4 q1, float4 q2)
{
	float w = q1.w * q2.w - dot(q1.xyz, q2.xyz);
	q1.xyz = q2.xyz * q1.w + q1.xyz * q2.w + cross(q1.xyz, q2.xyz);
	q1.w = w;
	return q1;
}

struct dual_quaternion DualQuaternionMultiply(struct dual_quaternion dq1, struct dual_quaternion dq2)
{
	struct dual_quaternion result;

	result.translation_quaternion = QuaternionMultiply(dq1.rotation_quaternion,		dq2.translation_quaternion) + 
									QuaternionMultiply(dq1.translation_quaternion,	dq2.rotation_quaternion);
	
	result.rotation_quaternion = QuaternionMultiply(dq1.rotation_quaternion, dq2.rotation_quaternion);

	float mag = length(result.rotation_quaternion);
	result.rotation_quaternion /= mag;
	result.translation_quaternion /= mag;

	return result;
}

struct dual_quaternion DualQuaternionShortestPath(struct dual_quaternion dq1, struct dual_quaternion dq2)
{
	bool isBadPath = dot(dq1.rotation_quaternion, dq2.rotation_quaternion) < 0;
	dq1.rotation_quaternion		= isBadPath ? -dq1.rotation_quaternion		: dq1.rotation_quaternion;
	dq1.translation_quaternion	= isBadPath ? -dq1.translation_quaternion	: dq1.translation_quaternion;
	return dq1;
}

float4 QuaternionApplyRotation(float4 v, float4 rotQ)
{
	v = QuaternionMultiply(rotQ, v);
	return QuaternionMultiply(v, QuaternionInvert(rotQ));
}

inline float signNoZero(float x)
{
	float s = sign(x);
	if (s)
		return s;
	return 1;
}

struct dual_quaternion DualQuaternionFromMatrix4x4(float4x4 m)
{
	struct  dual_quaternion dq;

	// http://www.euclideanspace.com/maths/geometry/rotations/conversions/matrixToQuaternion/index.htm
	// Alternative Method by Christian
	dq.rotation_quaternion.w = sqrt(max(0, 1.0 + m[0][0] + m[1][1] + m[2][2])) / 2.0;
	dq.rotation_quaternion.x = sqrt(max(0, 1.0 + m[0][0] - m[1][1] - m[2][2])) / 2.0;
	dq.rotation_quaternion.y = sqrt(max(0, 1.0 - m[0][0] + m[1][1] - m[2][2])) / 2.0;
	dq.rotation_quaternion.z = sqrt(max(0, 1.0 - m[0][0] - m[1][1] + m[2][2])) / 2.0;
	dq.rotation_quaternion.x *= signNoZero(m[2][1] - m[1][2]);
	dq.rotation_quaternion.y *= signNoZero(m[0][2] - m[2][0]);
	dq.rotation_quaternion.z *= signNoZero(m[1][0] - m[0][1]);

	dq.rotation_quaternion = normalize(dq.rotation_quaternion);	// ensure unit quaternion

	dq.translation_quaternion = float4(m[0][3], m[1][3], m[2][3], 0);
	dq.translation_quaternion = QuaternionMultiply(dq.translation_quaternion, dq.rotation_quaternion) * 0.5;

	return dq;
}
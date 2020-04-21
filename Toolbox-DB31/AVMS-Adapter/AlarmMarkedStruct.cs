using System.Xml.Serialization;

namespace Toolbox_DB31.AVMS_Adapter
{
    public struct AlarmMarkedStruct
    {
        public uint m_aCameraId;
        public uint m_bTmAlarm;
        public uint m_cTmAlarmMs;
        public uint m_dUserId;
        public uint m_eTmAck;
        public uint m_fAlarmTypeId;
        public uint m_gIsFalsePositive;
        public int m_hPolicyId;
        public uint m_iAlarmId;
        public uint m_jUnknown1;
        public uint m_jUnknown2;
        public uint m_jUnknown3;
        public uint m_jUnknown4;
    }
}

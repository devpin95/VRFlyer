using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// from 
// https://timaksu.com/post/125930865807/til-animation-curves-are-not-thread-safe-and-a

namespace Classes
{
    [System.Serializable]
    public class ThreadsafeCurve : ISerializationCallbackReceiver
    {
        [SerializeField] private AnimationCurve curve;
        [SerializeField] public int samples = 100;
        private List<float> _values = new List<float>();
        
        
        public void OnBeforeSerialize()
        {

        }

        public void OnAfterDeserialize()
        {
            UpdateValues();
        }
        
        private void UpdateValues()
        {
            _values = new List<float>();
            
            for (int i = 0; i < samples; ++i)
            {
                _values.Add( curve.Evaluate((float)i/samples) );
            }
        }

        public float Evaluate(float time)
        {
            time = Mathf.Clamp01(time);
            int index = (int)(time * samples);
            return _values[index];
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Imaging
{
    public class ImageProducerConfiguration
    {
        IDictionary<String, Object> config = new Dictionary<String, Object>();
        
        private ImageProducerConfiguration() { }

        public static ImageProducerConfiguration Simple(String key)
        {
            return Simple(key, new object());
        }

        public static ImageProducerConfiguration Simple(String key, Object value)
        {
            return Builder.addItem(key, value).Build();
        }

        public static ConfigBuilder Builder
        {
            get
            {
                return new ConfigBuilder();
            }
        }

        public bool ContainsKey(String key) { return config.ContainsKey(key); }

        public Object Get(String key) { return config[key]; }

        public class ConfigBuilder
        {
            private ImageProducerConfiguration _instance = new ImageProducerConfiguration();

            public ConfigBuilder addItem(String key, Object value)
            {
                _instance.config.Add(key, value);
                return this;
            }

            public ImageProducerConfiguration Build() { return _instance; }
        }

    }

}

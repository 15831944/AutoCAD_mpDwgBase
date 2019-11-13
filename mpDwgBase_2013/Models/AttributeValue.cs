namespace mpDwgBase.Models
{
    using System.Xml.Serialization;

    /// <summary>
    /// Атрибут блока
    /// </summary>
    public class AttributeValue
    {
        /// <summary>
        /// Тэг
        /// </summary>
        [XmlAttribute]
        public string Tag { get; set; }

        /// <summary>
        /// Подсказка
        /// </summary>
        [XmlAttribute]
        public string Prompt { get; set; }

        /// <summary>
        /// Строковое содержимое
        /// </summary>
        [XmlAttribute]
        public string TextString { get; set; }
    }
}
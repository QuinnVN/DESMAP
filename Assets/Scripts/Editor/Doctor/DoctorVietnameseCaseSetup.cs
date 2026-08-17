using System;
using System.Collections.Generic;
using NCKH.VR.Doctor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NCKH.VR.Doctor.Editor
{
    public static class DoctorVietnameseCaseSetup
    {
        private const string DataFolder = "Assets/Data/Doctor";
        private const string DatabasePath = DataFolder + "/Doctor Game 2 Vietnamese Case Database.asset";
        private const string ScenePath = "Assets/Scenes/Doctor/Game 2.unity";

        [MenuItem("Tools/NCKH VR/Doctor/Generate Vietnamese Game 2 Cases")]
        public static void Generate()
        {
            EnsureFolder(DataFolder);
            DoctorCaseDefinition[] cases = CreateCases();
            DoctorCaseDatabase database = CreateOrUpdateDatabase(cases);
            AssignDatabaseToGameTwo(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Generated {cases.Length} Vietnamese Doctor Game 2 cases at '{DataFolder}'.");
        }

        private static DoctorCaseDefinition[] CreateCases()
        {
            return new[]
            {
                Case("01 - Cum.asset", "Nguyễn Minh Anh",
                    Dialogue("Từ tối qua tôi đột ngột sốt, rét run, đau nhức toàn thân và ho khan. Tôi còn bỏ lỡ một bộ phim đang xem.",
                        Card("flu-time", "Khởi phát đột ngột từ tối qua", DoctorInfoCategory.Time),
                        Card("flu-fever", "Sốt và rét run", DoctorInfoCategory.Symptoms),
                        Card("flu-ache", "Đau nhức toàn thân", DoctorInfoCategory.Symptoms),
                        Card("flu-cough", "Ho khan", DoctorInfoCategory.Symptoms),
                        Noise("flu-movie", "Bỏ lỡ một bộ phim")),
                    Phase("flu-q1", "Cơn bệnh bắt đầu nhanh hay từ từ?", "Tôi đang bình thường thì đến tối qua các triệu chứng xuất hiện rất nhanh.", Card("flu-sudden", "Triệu chứng xuất hiện nhanh", DoctorInfoCategory.Time)),
                    Phase("flu-q2", "Anh có tiếp xúc với người đang bị bệnh không?", "Ba ngày trước, đồng nghiệp ngồi cạnh tôi cũng sốt và ho.", Card("flu-contact", "Tiếp xúc đồng nghiệp sốt và ho ba ngày trước", DoctorInfoCategory.TravelHistory)),
                    Phase("flu-q3", "Anh còn triệu chứng nào khác không?", "Tôi đau đầu, rất mệt và hơi đau họng, nhưng không bị tiêu chảy.", Card("flu-headache", "Đau đầu, mệt và đau họng", DoctorInfoCategory.Symptoms), Card("flu-no-diarrhea", "Không tiêu chảy", DoctorInfoCategory.Symptoms)),
                    Phase("flu-q4", "Anh có bệnh nền hoặc đã tiêm phòng cúm chưa?", "Tôi không có bệnh nền và năm nay chưa tiêm vắc-xin cúm.", Card("flu-history", "Không có bệnh nền", DoctorInfoCategory.MedicalHistory), Card("flu-vaccine", "Chưa tiêm vắc-xin cúm năm nay", DoctorInfoCategory.MedicalHistory))),

                Case("02 - COVID-19.asset", "Trần Hoàng Nam",
                    Dialogue("Tôi ho, đau họng và mệt từ hai ngày trước. Sáng nay tôi nhận ra đồ ăn gần như không còn mùi vị.",
                        Card("covid-time", "Khởi phát hai ngày trước", DoctorInfoCategory.Time),
                        Card("covid-cough", "Ho và đau họng", DoctorInfoCategory.Symptoms),
                        Card("covid-fatigue", "Mệt mỏi", DoctorInfoCategory.Symptoms),
                        Card("covid-smell", "Giảm khứu giác và vị giác", DoctorInfoCategory.Symptoms)),
                    Phase("covid-q1", "Anh có sốt hoặc khó thở không?", "Tôi sốt nhẹ 38 độ và hơi hụt hơi khi leo cầu thang, nhưng lúc ngồi nghỉ vẫn thở được.", Card("covid-fever", "Sốt nhẹ 38 độ", DoctorInfoCategory.Symptoms), Card("covid-breath", "Hụt hơi khi gắng sức", DoctorInfoCategory.Symptoms)),
                    Phase("covid-q2", "Gần đây anh có tiếp xúc với ca bệnh không?", "Bốn ngày trước tôi dự cuộc họp; sau đó một người báo xét nghiệm COVID-19 dương tính.", Card("covid-contact", "Tiếp xúc ca COVID-19 bốn ngày trước", DoctorInfoCategory.TravelHistory)),
                    Phase("covid-q3", "Anh đã làm xét nghiệm chưa?", "Tôi thử test nhanh sáng nay và thấy hai vạch.", Card("covid-test", "Test nhanh dương tính", DoctorInfoCategory.MedicalHistory)),
                    Phase("covid-q4", "Anh có bệnh nền hoặc tiền sử tiêm chủng thế nào?", "Tôi bị tăng huyết áp và mũi vắc-xin COVID-19 gần nhất đã hơn một năm.", Card("covid-hypertension", "Tiền sử tăng huyết áp", DoctorInfoCategory.MedicalHistory), Card("covid-vaccine", "Mũi vắc-xin gần nhất hơn một năm", DoctorInfoCategory.MedicalHistory))),

                Case("03 - Sot xuat huyet Dengue.asset", "Lê Thu Hà",
                    Dialogue("Tôi sốt cao liên tục từ ba ngày nay, đau đầu dữ dội, đau sau hai mắt và nhức mỏi khắp người.",
                        Card("dengue-time", "Sốt cao liên tục ba ngày", DoctorInfoCategory.Time),
                        Card("dengue-head", "Đau đầu dữ dội", DoctorInfoCategory.Symptoms),
                        Card("dengue-eyes", "Đau sau hai mắt", DoctorInfoCategory.Symptoms),
                        Card("dengue-ache", "Đau nhức cơ thể", DoctorInfoCategory.Symptoms)),
                    Phase("dengue-q1", "Chị có buồn nôn, phát ban hoặc chảy máu bất thường không?", "Tôi buồn nôn, trên tay mới nổi vài chấm đỏ nhưng chưa chảy máu mũi hay chân răng.", Card("dengue-nausea", "Buồn nôn", DoctorInfoCategory.Symptoms), Card("dengue-rash", "Xuất hiện chấm đỏ trên tay", DoctorInfoCategory.Symptoms), Card("dengue-no-bleed", "Chưa có chảy máu bất thường", DoctorInfoCategory.Symptoms)),
                    Phase("dengue-q2", "Gần đây chị có bị muỗi đốt hoặc đến nơi đang có dịch không?", "Khu phố tôi có nhiều muỗi sau mưa và tuần trước đã có người mắc sốt xuất huyết.", Card("dengue-exposure", "Nhiều muỗi và có ca sốt xuất huyết trong khu phố", DoctorInfoCategory.TravelHistory)),
                    Phase("dengue-q3", "Chị có đau bụng nhiều, nôn liên tục hay thấy lả đi không?", "Tôi chỉ hơi khó chịu ở bụng, chưa nôn và vẫn uống được nước.", Card("dengue-warning", "Chưa có dấu hiệu cảnh báo rõ", DoctorInfoCategory.Symptoms)),
                    Phase("dengue-q4", "Chị từng mắc sốt xuất huyết hoặc có bệnh nền chưa?", "Tôi từng mắc sốt xuất huyết cách đây sáu năm và không có bệnh nền khác.", Card("dengue-history", "Từng mắc sốt xuất huyết sáu năm trước", DoctorInfoCategory.MedicalHistory))),

                Case("04 - Hen phe quan.asset", "Phạm Gia Bảo",
                    Dialogue("Từ chiều nay tôi khò khè, nặng ngực và khó thở sau khi dọn một căn phòng đầy bụi.",
                        Card("asthma-time", "Khởi phát chiều nay", DoctorInfoCategory.Time),
                        Card("asthma-wheeze", "Khò khè", DoctorInfoCategory.Symptoms),
                        Card("asthma-chest", "Nặng ngực và khó thở", DoctorInfoCategory.Symptoms),
                        Card("asthma-dust", "Khởi phát sau khi tiếp xúc bụi", DoctorInfoCategory.TravelHistory)),
                    Phase("asthma-q1", "Khó thở xuất hiện liên tục hay từng cơn?", "Nó đến thành từng cơn, rõ hơn khi tôi thở ra và nói câu dài.", Card("asthma-pattern", "Khó thở từng cơn, khó thở ra", DoctorInfoCategory.Symptoms)),
                    Phase("asthma-q2", "Anh có sốt hoặc ho ra đờm không?", "Tôi ho khan nhưng không sốt và không có đờm.", Card("asthma-dry", "Ho khan, không sốt, không đờm", DoctorInfoCategory.Symptoms)),
                    Phase("asthma-q3", "Trước đây anh từng bị như vậy chưa?", "Tôi được chẩn đoán hen từ nhỏ và thỉnh thoảng lên cơn khi gặp bụi hoặc lông mèo.", Card("asthma-history", "Tiền sử hen từ nhỏ", DoctorInfoCategory.MedicalHistory), Card("asthma-trigger", "Bụi và lông mèo từng gây cơn", DoctorInfoCategory.MedicalHistory)),
                    Phase("asthma-q4", "Anh đã dùng thuốc cắt cơn chưa?", "Tôi quên mang bình xịt nên chưa dùng thuốc hôm nay.", Card("asthma-inhaler", "Chưa dùng bình xịt cắt cơn hôm nay", DoctorInfoCategory.MedicalHistory))),

                Case("05 - Dai thao duong type 2.asset", "Vũ Đức Long",
                    Dialogue("Khoảng ba tháng nay tôi hay khát nước, đi tiểu nhiều, mau đói và thường mệt vào buổi chiều.",
                        Card("dm-time", "Triệu chứng kéo dài khoảng ba tháng", DoctorInfoCategory.Time),
                        Card("dm-thirst", "Khát nước nhiều", DoctorInfoCategory.Symptoms),
                        Card("dm-urine", "Đi tiểu nhiều", DoctorInfoCategory.Symptoms),
                        Card("dm-fatigue", "Mau đói và mệt", DoctorInfoCategory.Symptoms)),
                    Phase("dm-q1", "Cân nặng và thị lực gần đây có thay đổi không?", "Tôi giảm ba cân dù không ăn kiêng và đôi lúc nhìn hơi mờ.", Card("dm-weight", "Sụt ba cân không chủ ý", DoctorInfoCategory.Symptoms), Card("dm-vision", "Đôi lúc nhìn mờ", DoctorInfoCategory.Symptoms)),
                    Phase("dm-q2", "Anh có vết thương lâu lành hoặc nhiễm trùng tái diễn không?", "Vết xước ở chân của tôi mất gần ba tuần mới lành.", Card("dm-heal", "Vết thương lâu lành", DoctorInfoCategory.Symptoms)),
                    Phase("dm-q3", "Trong gia đình có ai mắc đái tháo đường không?", "Mẹ tôi mắc đái tháo đường type 2 và đang uống thuốc mỗi ngày.", Card("dm-family", "Mẹ mắc đái tháo đường type 2", DoctorInfoCategory.MedicalHistory)),
                    Phase("dm-q4", "Lần gần nhất anh kiểm tra đường huyết là khi nào?", "Hôm qua tôi đo đường huyết lúc đói ở nhà được 8,4 mmol/L, nhưng chưa đi khám xác nhận.", Card("dm-glucose", "Đường huyết đói tại nhà 8,4 mmol/L", DoctorInfoCategory.MedicalHistory))),

                Case("06 - Nhiem trung duong tieu.asset", "Đặng Ngọc Mai",
                    Dialogue("Từ hôm qua tôi tiểu buốt, cứ muốn đi tiểu liên tục nhưng mỗi lần chỉ được rất ít.",
                        Card("uti-time", "Khởi phát từ hôm qua", DoctorInfoCategory.Time),
                        Card("uti-dysuria", "Tiểu buốt", DoctorInfoCategory.Symptoms),
                        Card("uti-frequency", "Tiểu nhiều lần, lượng ít", DoctorInfoCategory.Symptoms)),
                    Phase("uti-q1", "Nước tiểu có gì khác thường không?", "Nước tiểu đục hơn, mùi nặng và có lần hơi hồng.", Card("uti-urine", "Nước tiểu đục, mùi nặng, đôi lúc hơi hồng", DoctorInfoCategory.Symptoms)),
                    Phase("uti-q2", "Chị có sốt, rét run hoặc đau vùng hông lưng không?", "Tôi không sốt, không rét run và không đau hông lưng.", Card("uti-no-upper", "Không sốt, rét run hay đau hông lưng", DoctorInfoCategory.Symptoms)),
                    Phase("uti-q3", "Gần đây chị có đi xa hoặc phải nhịn tiểu lâu không?", "Tôi vừa đi xe đường dài và đã nhịn tiểu gần sáu tiếng.", Card("uti-travel", "Đi đường dài và nhịn tiểu gần sáu tiếng", DoctorInfoCategory.TravelHistory)),
                    Phase("uti-q4", "Chị từng bị nhiễm trùng đường tiểu hoặc đang mang thai không?", "Tôi từng bị một lần năm ngoái và hiện không mang thai.", Card("uti-history", "Từng nhiễm trùng đường tiểu năm ngoái", DoctorInfoCategory.MedicalHistory), Card("uti-pregnancy", "Hiện không mang thai", DoctorInfoCategory.MedicalHistory))),

                Case("07 - Cam lanh.asset", "Bùi Khánh Linh",
                    Dialogue("Ba ngày nay tôi sổ mũi, nghẹt mũi, hắt hơi và hơi đau họng; tôi chỉ sốt nhẹ một lần.",
                        Card("cold-time", "Triệu chứng kéo dài ba ngày", DoctorInfoCategory.Time),
                        Card("cold-nose", "Sổ mũi, nghẹt mũi và hắt hơi", DoctorInfoCategory.Symptoms),
                        Card("cold-throat", "Đau họng nhẹ", DoctorInfoCategory.Symptoms),
                        Card("cold-fever", "Chỉ sốt nhẹ một lần", DoctorInfoCategory.Symptoms)),
                    Phase("cold-q1", "Các triệu chứng xuất hiện như thế nào?", "Ban đầu tôi rát họng, hôm sau mới nghẹt mũi và ho nhẹ.", Card("cold-course", "Rát họng trước, sau đó nghẹt mũi và ho nhẹ", DoctorInfoCategory.Time)),
                    Phase("cold-q2", "Gần đây chị có tiếp xúc với người có triệu chứng tương tự không?", "Con tôi bị sổ mũi vài ngày trước, ngoài ra tôi không đi xa.", Card("cold-contact", "Tiếp xúc con bị sổ mũi", DoctorInfoCategory.TravelHistory)),
                    Phase("cold-q3", "Chị có bệnh nền đáng chú ý không?", "Tôi khỏe mạnh, không hen và không có bệnh phổi mạn tính.", Card("cold-history", "Không có bệnh nền hô hấp", DoctorInfoCategory.MedicalHistory))),

                Case("08 - Viem phoi.asset", "Đỗ Văn Hùng",
                    Dialogue("Tôi sốt cao, ho có đờm vàng và đau nhói bên ngực phải mỗi khi hít sâu từ bốn ngày nay.",
                        Card("pne-time", "Triệu chứng kéo dài bốn ngày", DoctorInfoCategory.Time),
                        Card("pne-fever", "Sốt cao", DoctorInfoCategory.Symptoms),
                        Card("pne-sputum", "Ho đờm vàng", DoctorInfoCategory.Symptoms),
                        Card("pne-pain", "Đau ngực tăng khi hít sâu", DoctorInfoCategory.Symptoms)),
                    Phase("pne-q1", "Anh có khó thở hoặc thở nhanh không?", "Tôi hụt hơi ngay cả khi đi chậm và vợ tôi nói tôi thở nhanh hơn bình thường.", Card("pne-breath", "Khó thở và thở nhanh", DoctorInfoCategory.Symptoms)),
                    Phase("pne-q2", "Trước khi bệnh anh có đi xa hoặc tiếp xúc người bị bệnh không?", "Tôi không đi xa, nhưng cháu tôi vừa bị nhiễm trùng hô hấp tuần trước.", Card("pne-contact", "Tiếp xúc người thân bị nhiễm trùng hô hấp", DoctorInfoCategory.TravelHistory)),
                    Phase("pne-q3", "Anh có bệnh nền hoặc hút thuốc không?", "Tôi hút thuốc nhiều năm và được chẩn đoán bệnh phổi tắc nghẽn mạn tính.", Card("pne-smoking", "Hút thuốc nhiều năm", DoctorInfoCategory.MedicalHistory), Card("pne-copd", "Có bệnh phổi tắc nghẽn mạn tính", DoctorInfoCategory.MedicalHistory))),

                Case("09 - Viem phe quan cap.asset", "Hoàng Tuấn Kiệt",
                    Dialogue("Sau một đợt cảm, tôi ho nhiều đã sáu ngày, lúc đầu ho khan rồi chuyển sang có ít đờm.",
                        Card("bron-time", "Ho kéo dài sáu ngày sau cảm", DoctorInfoCategory.Time),
                        Card("bron-cough", "Ho từ khan chuyển sang có đờm", DoctorInfoCategory.Symptoms)),
                    Phase("bron-q1", "Anh có sốt cao, khó thở hay đau ngực không?", "Tôi chỉ sốt nhẹ, hơi rát sau xương ức khi ho, không khó thở lúc nghỉ.", Card("bron-detail", "Sốt nhẹ và rát ngực khi ho", DoctorInfoCategory.Symptoms), Card("bron-no-dyspnea", "Không khó thở lúc nghỉ", DoctorInfoCategory.Symptoms)),
                    Phase("bron-q2", "Anh có tiếp xúc khói bụi hoặc người đang bệnh không?", "Tôi làm ở nơi có nhiều khói và vài đồng nghiệp cũng đang ho.", Card("bron-exposure", "Tiếp xúc khói và đồng nghiệp đang ho", DoctorInfoCategory.TravelHistory)),
                    Phase("bron-q3", "Anh có hen, bệnh phổi hoặc hút thuốc không?", "Tôi không có bệnh phổi nhưng hút khoảng năm điếu thuốc mỗi ngày.", Card("bron-history", "Hút khoảng năm điếu thuốc mỗi ngày", DoctorInfoCategory.MedicalHistory))),

                Case("10 - Viem da day ruot.asset", "Ngô Thanh Thảo",
                    Dialogue("Từ tối qua tôi đau quặn bụng, nôn hai lần và đi ngoài phân lỏng nhiều lần.",
                        Card("gastro-time", "Khởi phát từ tối qua", DoctorInfoCategory.Time),
                        Card("gastro-cramp", "Đau quặn bụng", DoctorInfoCategory.Symptoms),
                        Card("gastro-vomit", "Nôn và tiêu chảy", DoctorInfoCategory.Symptoms)),
                    Phase("gastro-q1", "Chị có sốt, có máu trong phân hay dấu mất nước không?", "Tôi sốt nhẹ, không thấy máu nhưng miệng khô và tiểu ít hơn.", Card("gastro-fever", "Sốt nhẹ, không có máu trong phân", DoctorInfoCategory.Symptoms), Card("gastro-dehydration", "Miệng khô và tiểu ít", DoctorInfoCategory.Symptoms)),
                    Phase("gastro-q2", "Có ai quanh chị bị giống vậy hoặc chị vừa đi đâu không?", "Hai người trong nhà cũng tiêu chảy sau khi một cháu nhỏ ở trường mẫu giáo bị bệnh.", Card("gastro-contact", "Nhiều người trong nhà có triệu chứng tương tự", DoctorInfoCategory.TravelHistory)),
                    Phase("gastro-q3", "Chị có bệnh nền hoặc đang dùng thuốc gì không?", "Tôi không có bệnh nền và gần đây không dùng kháng sinh.", Card("gastro-history", "Không bệnh nền, không dùng kháng sinh gần đây", DoctorInfoCategory.MedicalHistory))),

                Case("11 - Ngo doc thuc pham.asset", "Dương Quốc Việt",
                    Dialogue("Khoảng sáu giờ sau bữa tiệc hải sản, tôi buồn nôn, nôn và đau bụng dữ dội.",
                        Card("food-time", "Khởi phát sáu giờ sau bữa tiệc", DoctorInfoCategory.Time),
                        Card("food-nausea", "Buồn nôn và nôn", DoctorInfoCategory.Symptoms),
                        Card("food-pain", "Đau bụng dữ dội", DoctorInfoCategory.Symptoms)),
                    Phase("food-q1", "Anh có tiêu chảy, sốt hoặc máu trong phân không?", "Tôi tiêu chảy nhiều lần, hơi sốt nhưng không thấy máu.", Card("food-diarrhea", "Tiêu chảy nhiều lần và sốt nhẹ", DoctorInfoCategory.Symptoms), Card("food-no-blood", "Không thấy máu trong phân", DoctorInfoCategory.Symptoms)),
                    Phase("food-q2", "Những người ăn cùng có bị tương tự không?", "Ba người cùng ăn món hàu sống cũng nôn và tiêu chảy trong đêm.", Card("food-cluster", "Nhiều người ăn hàu sống cùng mắc bệnh", DoctorInfoCategory.TravelHistory)),
                    Phase("food-q3", "Anh có bệnh nền hoặc dị ứng hải sản không?", "Tôi không dị ứng hải sản, nhưng đang dùng thuốc giảm acid dạ dày mỗi ngày.", Card("food-history", "Đang dùng thuốc giảm acid dạ dày", DoctorInfoCategory.MedicalHistory))),

                Case("12 - Trao nguoc da day thuc quan.asset", "Phan Mỹ Duyên",
                    Dialogue("Gần một tháng nay tôi hay nóng rát sau xương ức và ợ chua, nhất là sau bữa tối.",
                        Card("gerd-time", "Triệu chứng gần một tháng", DoctorInfoCategory.Time),
                        Card("gerd-heartburn", "Nóng rát sau xương ức", DoctorInfoCategory.Symptoms),
                        Card("gerd-regurg", "Ợ chua sau ăn", DoctorInfoCategory.Symptoms)),
                    Phase("gerd-q1", "Triệu chứng thay đổi theo tư thế hoặc thức ăn thế nào?", "Nằm xuống ngay sau ăn thì nặng hơn; cà phê và đồ cay cũng làm tôi khó chịu.", Card("gerd-trigger", "Nặng hơn khi nằm sau ăn, uống cà phê hoặc ăn cay", DoctorInfoCategory.Time)),
                    Phase("gerd-q2", "Chị có khó nuốt, nôn ra máu hoặc sụt cân không?", "Tôi không khó nuốt, không nôn ra máu và cân nặng không đổi.", Card("gerd-redflags", "Không khó nuốt, xuất huyết hay sụt cân", DoctorInfoCategory.Symptoms)),
                    Phase("gerd-q3", "Chị có bệnh nền hoặc đang dùng thuốc gì?", "Tôi thừa cân và thỉnh thoảng tự mua thuốc kháng acid khi bị nóng rát.", Card("gerd-history", "Thừa cân và thỉnh thoảng dùng thuốc kháng acid", DoctorInfoCategory.MedicalHistory))),

                Case("13 - Viem mui di ung.asset", "Mai Anh Khoa",
                    Dialogue("Mỗi sáng tôi hắt hơi liên tục, ngứa mũi, chảy nước mũi trong và ngứa mắt nhưng không sốt.",
                        Card("allergy-time", "Tái diễn vào mỗi buổi sáng", DoctorInfoCategory.Time),
                        Card("allergy-sneeze", "Hắt hơi, ngứa mũi và chảy mũi trong", DoctorInfoCategory.Symptoms),
                        Card("allergy-eyes", "Ngứa mắt", DoctorInfoCategory.Symptoms),
                        Card("allergy-no-fever", "Không sốt", DoctorInfoCategory.Symptoms)),
                    Phase("allergy-q1", "Triệu chứng có liên quan đến môi trường nào không?", "Tôi nặng hơn khi quét nhà hoặc đến nơi có nhiều phấn hoa, ra ngoài thoáng thì đỡ.", Card("allergy-trigger", "Nặng hơn khi gặp bụi và phấn hoa", DoctorInfoCategory.TravelHistory)),
                    Phase("allergy-q2", "Anh có đau họng, đau nhức hoặc dịch mũi đặc không?", "Tôi không đau nhức, chỉ hơi nghẹt mũi và dịch luôn trong.", Card("allergy-detail", "Dịch mũi trong, không đau nhức", DoctorInfoCategory.Symptoms)),
                    Phase("allergy-q3", "Anh có tiền sử dị ứng hoặc hen không?", "Tôi bị dị ứng mạt bụi từ nhỏ, mẹ tôi cũng bị viêm mũi dị ứng.", Card("allergy-history", "Tiền sử dị ứng mạt bụi", DoctorInfoCategory.MedicalHistory), Card("allergy-family", "Mẹ bị viêm mũi dị ứng", DoctorInfoCategory.MedicalHistory))),

                Case("14 - Thuy dau.asset", "Lý Bảo Ngọc",
                    Dialogue("Hai ngày nay tôi sốt nhẹ, mệt và nổi nhiều nốt đỏ rất ngứa, sau đó một số nốt biến thành bóng nước.",
                        Card("pox-time", "Phát ban trong hai ngày", DoctorInfoCategory.Time),
                        Card("pox-fever", "Sốt nhẹ và mệt", DoctorInfoCategory.Symptoms),
                        Card("pox-rash", "Nốt đỏ ngứa tiến triển thành bóng nước", DoctorInfoCategory.Symptoms)),
                    Phase("pox-q1", "Các nốt bắt đầu ở đâu và có xuất hiện thành nhiều đợt không?", "Nốt bắt đầu ở thân mình rồi lan lên mặt; hiện có cả nốt đỏ mới lẫn bóng nước và nốt đóng vảy.", Card("pox-pattern", "Ban từ thân mình lan ra, nhiều giai đoạn cùng lúc", DoctorInfoCategory.Symptoms)),
                    Phase("pox-q2", "Gần đây em có tiếp xúc với ai bị phát ban không?", "Khoảng hai tuần trước em chơi cùng em họ, sau đó bạn ấy được chẩn đoán thủy đậu.", Card("pox-contact", "Tiếp xúc ca thủy đậu khoảng hai tuần trước", DoctorInfoCategory.TravelHistory)),
                    Phase("pox-q3", "Em đã tiêm vắc-xin hoặc từng bị thủy đậu chưa?", "Em chưa từng mắc và gia đình không nhớ em đã tiêm vắc-xin thủy đậu.", Card("pox-history", "Chưa từng mắc, không rõ tiền sử tiêm vắc-xin", DoctorInfoCategory.MedicalHistory))),

                Case("15 - Viem ket mac.asset", "Trịnh Hải Yến",
                    Dialogue("Từ sáng nay mắt trái tôi đỏ, cộm như có cát và chảy nhiều nước mắt.",
                        Card("conj-time", "Khởi phát sáng nay", DoctorInfoCategory.Time),
                        Card("conj-red", "Mắt trái đỏ và cộm", DoctorInfoCategory.Symptoms),
                        Card("conj-tear", "Chảy nhiều nước mắt", DoctorInfoCategory.Symptoms)),
                    Phase("conj-q1", "Mắt có ghèn, ngứa hoặc đau nhiều không?", "Mắt hơi ngứa, sáng ngủ dậy có ghèn vàng làm dính mi, nhưng không đau sâu trong mắt.", Card("conj-discharge", "Ngứa nhẹ và có ghèn vàng", DoctorInfoCategory.Symptoms), Card("conj-no-pain", "Không đau sâu trong mắt", DoctorInfoCategory.Symptoms)),
                    Phase("conj-q2", "Chị có tiếp xúc với người đau mắt hoặc dùng chung đồ cá nhân không?", "Con tôi cũng đỏ mắt từ hai ngày trước và chúng tôi dùng chung khăn mặt.", Card("conj-contact", "Tiếp xúc người đỏ mắt và dùng chung khăn", DoctorInfoCategory.TravelHistory)),
                    Phase("conj-q3", "Chị có đeo kính áp tròng hoặc bệnh mắt trước đây không?", "Tôi không đeo kính áp tròng và trước đây mắt hoàn toàn bình thường.", Card("conj-history", "Không đeo kính áp tròng, không bệnh mắt trước đây", DoctorInfoCategory.MedicalHistory))),

                Case("16 - Dau nua dau.asset", "Tạ Minh Châu",
                    Dialogue("Từ sáng tôi đau giật theo nhịp ở nửa đầu bên phải, ánh sáng và tiếng động làm đau tăng lên.",
                        Card("mig-time", "Khởi phát từ sáng", DoctorInfoCategory.Time),
                        Card("mig-pulse", "Đau giật một bên đầu", DoctorInfoCategory.Symptoms),
                        Card("mig-sensory", "Nhạy với ánh sáng và tiếng động", DoctorInfoCategory.Symptoms)),
                    Phase("mig-q1", "Trước hoặc trong cơn chị có buồn nôn hay rối loạn thị giác không?", "Tôi buồn nôn và trước khi đau thấy những đường sáng ngoằn ngoèo khoảng hai mươi phút.", Card("mig-nausea", "Buồn nôn", DoctorInfoCategory.Symptoms), Card("mig-aura", "Có tiền triệu thị giác khoảng hai mươi phút", DoctorInfoCategory.Symptoms)),
                    Phase("mig-q2", "Gần đây chị có thay đổi sinh hoạt hay đi xa không?", "Tôi vừa đi công tác, ngủ rất ít và bỏ bữa sáng hôm nay.", Card("mig-trigger", "Đi công tác, thiếu ngủ và bỏ bữa", DoctorInfoCategory.TravelHistory)),
                    Phase("mig-q3", "Chị từng có cơn tương tự hoặc có dấu hiệu thần kinh bất thường không?", "Tôi đã có vài cơn giống vậy; lần này không yếu liệt, không méo miệng và không nói khó.", Card("mig-history", "Từng có nhiều cơn tương tự", DoctorInfoCategory.MedicalHistory), Card("mig-no-neuro", "Không có dấu thần kinh khu trú", DoctorInfoCategory.Symptoms))),

                Case("17 - Thoai hoa khop goi.asset", "Cao Văn Thành",
                    Dialogue("Khoảng một năm nay đầu gối phải tôi đau âm ỉ, nhất là khi đi bộ lâu hoặc lên cầu thang.",
                        Card("oa-time", "Đau kéo dài khoảng một năm", DoctorInfoCategory.Time),
                        Card("oa-knee", "Đau đầu gối khi vận động", DoctorInfoCategory.Symptoms)),
                    Phase("oa-q1", "Khớp có cứng vào buổi sáng hoặc sưng nóng đỏ không?", "Sáng ngủ dậy khớp cứng khoảng mười phút rồi đỡ; khớp không đỏ và chỉ hơi sưng sau khi đi nhiều.", Card("oa-stiff", "Cứng khớp buổi sáng khoảng mười phút", DoctorInfoCategory.Symptoms), Card("oa-swelling", "Chỉ sưng nhẹ sau vận động, không đỏ", DoctorInfoCategory.Symptoms)),
                    Phase("oa-q2", "Cơn đau thay đổi thế nào khi nghỉ ngơi?", "Nghỉ thì đỡ, nhưng tôi nghe tiếng lạo xạo khi co duỗi gối.", Card("oa-mechanical", "Đau giảm khi nghỉ và có tiếng lạo xạo", DoctorInfoCategory.Symptoms)),
                    Phase("oa-q3", "Bác có chấn thương cũ hoặc yếu tố nguy cơ nào không?", "Tôi thừa cân và từng chấn thương đầu gối khi chơi bóng đá mười năm trước.", Card("oa-history", "Thừa cân và từng chấn thương đầu gối", DoctorInfoCategory.MedicalHistory))),

                Case("18 - Tang huyet ap.asset", "Nguyễn Thị Hạnh",
                    Dialogue("Tôi không thấy bệnh gì rõ rệt, nhưng ba lần đo huyết áp gần đây ở nhà đều khoảng 155 trên 95 mmHg.",
                        Card("htn-time", "Huyết áp cao qua ba lần đo gần đây", DoctorInfoCategory.Time),
                        Card("htn-reading", "Huyết áp tại nhà khoảng 155/95 mmHg", DoctorInfoCategory.Symptoms)),
                    Phase("htn-q1", "Chị có đau đầu dữ dội, đau ngực, khó thở hay nhìn mờ không?", "Tôi chỉ thỉnh thoảng đau đầu nhẹ, không đau ngực, không khó thở và không nhìn mờ.", Card("htn-symptoms", "Không có triệu chứng cấp tính đáng chú ý", DoctorInfoCategory.Symptoms)),
                    Phase("htn-q2", "Việc ăn uống, vận động và hút thuốc của chị thế nào?", "Tôi hay ăn mặn, ít vận động và không hút thuốc.", Card("htn-lifestyle", "Ăn mặn và ít vận động", DoctorInfoCategory.TravelHistory)),
                    Phase("htn-q3", "Chị có bệnh nền hoặc tiền sử gia đình không?", "Bố tôi bị tăng huyết áp; bản thân tôi thừa cân và có mỡ máu cao.", Card("htn-family", "Bố bị tăng huyết áp", DoctorInfoCategory.MedicalHistory), Card("htn-history", "Thừa cân và mỡ máu cao", DoctorInfoCategory.MedicalHistory))),

                Case("19 - Viem da day.asset", "Hồ Quang Phúc",
                    Dialogue("Ba ngày nay tôi đau rát vùng trên rốn, đầy bụng và buồn nôn, nhất là khi để bụng đói.",
                        Card("gastritis-time", "Triệu chứng kéo dài ba ngày", DoctorInfoCategory.Time),
                        Card("gastritis-pain", "Đau rát vùng thượng vị", DoctorInfoCategory.Symptoms),
                        Card("gastritis-nausea", "Đầy bụng và buồn nôn", DoctorInfoCategory.Symptoms)),
                    Phase("gastritis-q1", "Anh có nôn ra máu, đi ngoài phân đen hoặc sụt cân không?", "Tôi không nôn ra máu, phân vẫn màu bình thường và cân nặng không đổi.", Card("gastritis-redflags", "Không xuất huyết tiêu hóa hay sụt cân", DoctorInfoCategory.Symptoms)),
                    Phase("gastritis-q2", "Anh có dùng thuốc giảm đau, rượu bia hoặc từng bệnh dạ dày không?", "Tuần qua tôi uống ibuprofen vì đau lưng và uống bia gần như mỗi tối; trước đây từng viêm dạ dày.", Card("gastritis-nsaid", "Dùng ibuprofen trong tuần qua và uống bia thường xuyên", DoctorInfoCategory.MedicalHistory), Card("gastritis-history", "Tiền sử viêm dạ dày", DoctorInfoCategory.MedicalHistory))),

                Case("20 - Viem hong lien cau.asset", "Đinh Thùy Trang",
                    Dialogue("Tôi đột ngột sốt và đau họng dữ dội từ hôm qua, nuốt rất đau nhưng hầu như không ho.",
                        Card("strep-time", "Khởi phát đột ngột từ hôm qua", DoctorInfoCategory.Time),
                        Card("strep-fever", "Sốt và đau họng dữ dội", DoctorInfoCategory.Symptoms),
                        Card("strep-swallow", "Nuốt đau", DoctorInfoCategory.Symptoms),
                        Card("strep-no-cough", "Hầu như không ho", DoctorInfoCategory.Symptoms)),
                    Phase("strep-q1", "Chị có nổi hạch, chấm đỏ hoặc mủ ở họng không?", "Tôi sờ thấy hạch đau ở trước cổ; amidan có mảng trắng và vài chấm đỏ trên vòm miệng.", Card("strep-nodes", "Hạch cổ trước đau", DoctorInfoCategory.Symptoms), Card("strep-tonsil", "Amidan có mảng trắng và chấm đỏ vòm miệng", DoctorInfoCategory.Symptoms)),
                    Phase("strep-q2", "Chị có tiếp xúc người đau họng hoặc tiền sử dị ứng thuốc không?", "Em tôi được xác nhận viêm họng liên cầu ba ngày trước; tôi từng nổi mề đay khi dùng penicillin.", Card("strep-contact", "Tiếp xúc ca viêm họng liên cầu", DoctorInfoCategory.TravelHistory), Card("strep-allergy", "Tiền sử nổi mề đay với penicillin", DoctorInfoCategory.MedicalHistory)))
            };
        }

        private static DoctorCaseDefinition Case(string fileName, string patientName, DoctorDialogue opening, params DoctorQuestionPhase[] phases)
        {
            string path = DataFolder + "/" + fileName;
            DoctorCaseDefinition asset = AssetDatabase.LoadAssetAtPath<DoctorCaseDefinition>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<DoctorCaseDefinition>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.ConfigureInEditor(patientName, 1f, opening, phases);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static DoctorCaseDatabase CreateOrUpdateDatabase(IEnumerable<DoctorCaseDefinition> cases)
        {
            DoctorCaseDatabase database = AssetDatabase.LoadAssetAtPath<DoctorCaseDatabase>(DatabasePath);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<DoctorCaseDatabase>();
                AssetDatabase.CreateAsset(database, DatabasePath);
            }

            database.ConfigureInEditor(cases);
            EditorUtility.SetDirty(database);
            return database;
        }

        private static void AssignDatabaseToGameTwo(DoctorCaseDatabase database)
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            DoctorGameManager manager = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                manager = root.GetComponentInChildren<DoctorGameManager>(true);
                if (manager != null)
                    break;
            }

            if (manager == null)
                throw new InvalidOperationException($"No {nameof(DoctorGameManager)} was found in '{ScenePath}'.");

            SerializedObject serializedManager = new SerializedObject(manager);
            SerializedProperty databaseProperty = serializedManager.FindProperty("caseDatabase");
            if (databaseProperty == null)
                throw new InvalidOperationException("DoctorGameManager is missing its caseDatabase field.");

            databaseProperty.objectReferenceValue = database;
            serializedManager.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static DoctorDialogue Dialogue(string transcript, params DoctorInfoCard[] cards) =>
            new DoctorDialogue(transcript, null, cards);

        private static DoctorQuestionPhase Phase(string id, string question, string response, params DoctorInfoCard[] cards) =>
            new DoctorQuestionPhase(id, question, Dialogue(response, cards));

        private static DoctorInfoCard Card(string id, string text, DoctorInfoCategory category) =>
            new DoctorInfoCard(id, text, category, true);

        private static DoctorInfoCard Noise(string id, string text) =>
            new DoctorInfoCard(id, text, DoctorInfoCategory.Time, false);

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            int split = path.LastIndexOf('/');
            EnsureFolder(path.Substring(0, split));
            AssetDatabase.CreateFolder(path.Substring(0, split), path.Substring(split + 1));
        }
    }
}

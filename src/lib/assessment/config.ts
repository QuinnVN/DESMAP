import { careerInterestOptions, type CareerInterest } from '$lib/questionnaire';

export type GroupedDimensionId =
	| 'D1'
	| 'D2'
	| 'D3'
	| 'D4'
	| 'D5'
	| 'D6'
	| 'E1'
	| 'E2'
	| 'E3'
	| 'E4'
	| 'E5'
	| 'E6'
	| 'S1'
	| 'S2'
	| 'S3'
	| 'M1'
	| 'M2'
	| 'M3'
	| 'A1'
	| 'A2'
	| 'A3'
	| 'A4'
	| 'P1'
	| 'P2'
	| 'P3'
	| 'P4'
	| 'P5'
	| 'P6';

export type CareerId =
	'doctor' | 'lawyer' | 'teacher' | 'sales-representative' | 'automotive-engineer';

export type AssessmentDimension = {
	id: GroupedDimensionId;
	name: string;
	description: string;
};

export type AssessmentCareer = {
	id: CareerId;
	name: string;
	description: string;
	nameSlug: string;
	weights: Record<GroupedDimensionId, number>;
};

export type AssessmentConfig = {
	dimensions: readonly AssessmentDimension[];
	careers: readonly AssessmentCareer[];
	interestToCareers: Readonly<Record<string, readonly CareerId[]>>;
};

const dimensionRows: Array<[GroupedDimensionId, string, string]> = [
	[
		'D1',
		'Thu nhập, phúc lợi, ổn định',
		'Mức độ coi trọng thu nhập, phúc lợi và sự ổn định trong công việc.'
	],
	['D2', 'Học hỏi, phát triển, thử thách', 'Mong muốn học hỏi, phát triển và xử lý thử thách mới.'],
	['D3', 'Tự chủ', 'Mức độ mong muốn tự chủ trong cách tổ chức và thực hiện công việc.'],
	[
		'D4',
		'Ý nghĩa và đóng góp',
		'Mong muốn tạo ra ý nghĩa và đóng góp cho người khác hoặc cộng đồng.'
	],
	['D5', 'Công nhận và ảnh hưởng', 'Mong muốn được ghi nhận và tạo ảnh hưởng tích cực.'],
	['D6', 'Điều kiện và cân bằng công việc', 'Mức độ coi trọng điều kiện làm việc và sự cân bằng.'],
	['E1', 'Kỹ năng nền tảng', 'Năng lực nền tảng hỗ trợ học tập và thực hiện công việc.'],
	['E2', 'Giải quyết vấn đề phức tạp', 'Khả năng phân tích và giải quyết vấn đề phức tạp.'],
	['E3', 'Kỹ năng tương tác xã hội', 'Khả năng giao tiếp và phối hợp với người khác.'],
	['E4', 'Kỹ năng kỹ thuật', 'Khả năng sử dụng kiến thức và công cụ kỹ thuật.'],
	['E5', 'Kỹ năng hệ thống', 'Khả năng nhìn nhận và vận hành các hệ thống liên kết.'],
	['E6', 'Quản lý nguồn lực', 'Khả năng tổ chức thời gian, công cụ và nguồn lực.'],
	['S1', 'Vai trò hướng nhiệm vụ', 'Xu hướng tập trung vào mục tiêu và hoàn thành nhiệm vụ.'],
	['S2', 'Vai trò duy trì quan hệ', 'Xu hướng xây dựng và duy trì quan hệ hợp tác.'],
	['S3', 'Vai trò định hướng cá nhân', 'Xu hướng định hướng, hỗ trợ hoặc dẫn dắt cá nhân.'],
	['M1', 'Tư duy phân tích', 'Cách phân tích dữ kiện, cấu trúc và nguyên nhân.'],
	['M2', 'Tư duy sáng tạo', 'Cách tạo ra ý tưởng và góc nhìn mới.'],
	['M3', 'Tư duy thực tiễn', 'Cách chuyển ý tưởng thành hành động thực tế.'],
	['A1', 'Chuẩn bị cho tương lai', 'Mức độ chuẩn bị và định hướng cho những thay đổi phía trước.'],
	['A2', 'Chủ động và chịu trách nhiệm', 'Mức độ chủ động và sẵn sàng chịu trách nhiệm.'],
	['A3', 'Khám phá khả năng mới', 'Mức độ cởi mở với cơ hội và khả năng mới.'],
	['A4', 'Tự tin vượt qua khó khăn', 'Mức độ tự tin khi đối diện với trở ngại.'],
	['P1', 'Áp lực thời gian và tốc độ', 'Cách phản ứng với thời hạn và tốc độ làm việc.'],
	['P2', 'Áp lực khối lượng công việc', 'Cách phản ứng khi có nhiều việc cần xử lý.'],
	[
		'P3',
		'Áp lực tư duy và quyết định',
		'Cách phản ứng khi cần suy nghĩ và quyết định dưới áp lực.'
	],
	['P4', 'Áp lực cảm xúc', 'Cách phản ứng với cảm xúc căng thẳng trong công việc.'],
	['P5', 'Áp lực tương tác và xung đột', 'Cách phản ứng với tương tác khó và xung đột.'],
	[
		'P6',
		'Áp lực trách nhiệm và hậu quả',
		'Cách phản ứng với trách nhiệm và hậu quả của quyết định.'
	]
];

export const groupedDimensions: readonly AssessmentDimension[] = dimensionRows.map(
	([id, name, description]) => ({ id, name, description })
);

const careerRows: Array<[CareerId, string, string, string, number[]]> = [
	[
		'doctor',
		'Bác sĩ',
		'Khám, chẩn đoán và điều trị cho người bệnh.',
		'doctor',
		[3, 5, 2, 5, 2, 2, 5, 5, 5, 3, 4, 4, 5, 5, 2, 5, 3, 5, 4, 5, 4, 5, 5, 5, 5, 5, 4, 5]
	],
	[
		'lawyer',
		'Luật sư',
		'Phân tích vụ việc, lập luận và bảo vệ quyền lợi hợp pháp.',
		'lawyer',
		[3, 4, 4, 4, 4, 2, 5, 5, 5, 1, 4, 4, 5, 3, 4, 5, 4, 4, 4, 5, 4, 5, 5, 5, 5, 4, 5, 5]
	],
	[
		'teacher',
		'Giáo viên',
		'Giảng dạy, hướng dẫn và giúp người học phát triển.',
		'teacher',
		[2, 5, 3, 5, 3, 3, 5, 4, 5, 2, 4, 4, 4, 5, 2, 4, 5, 5, 4, 4, 5, 4, 4, 4, 4, 5, 5, 4]
	],
	[
		'sales-representative',
		'Nhân viên kinh doanh',
		'Xây dựng kết nối và phát triển giải pháp cùng khách hàng.',
		'sales-representative',
		[5, 4, 4, 3, 5, 2, 4, 4, 5, 2, 3, 5, 4, 5, 4, 4, 4, 5, 3, 5, 4, 5, 5, 5, 4, 5, 5, 4]
	],
	[
		'automotive-engineer',
		'Kỹ sư ô tô',
		'Thiết kế, chẩn đoán và giải quyết thử thách kỹ thuật.',
		'automotive-engineer',
		[4, 5, 4, 3, 3, 3, 4, 5, 3, 5, 5, 4, 5, 3, 3, 5, 4, 5, 4, 5, 5, 5, 4, 4, 5, 3, 3, 5]
	]
];

export const assessmentCareers: readonly AssessmentCareer[] = careerRows.map(
	([id, name, description, nameSlug, weights]) => ({
		id,
		name,
		description,
		nameSlug,
		weights: Object.fromEntries(
			groupedDimensions.map((dimension, index) => [dimension.id, weights[index]])
		) as Record<GroupedDimensionId, number>
	})
);

const mappedInterestIds: Record<string, readonly CareerId[]> = {
	'technology-engineering': ['automotive-engineer'],
	'environment-sustainability': ['automotive-engineer'],
	'operations-trades': ['automotive-engineer'],
	'science-research': ['doctor'],
	'health-wellbeing': ['doctor'],
	'design-creative': ['teacher'],
	'people-education': ['teacher'],
	'business-entrepreneurship': ['sales-representative'],
	'media-communication': ['sales-representative'],
	'law-public-service': ['lawyer'],
	exploring: assessmentCareers.map((career) => career.id)
};

export const assessmentConfig: AssessmentConfig = {
	dimensions: groupedDimensions,
	careers: assessmentCareers,
	interestToCareers: mappedInterestIds
};

export function careerCandidatesForInterests(
	interests: readonly CareerInterest[] | readonly string[]
): AssessmentCareer[] {
	const ids = new Set<CareerId>();
	const ordered: CareerId[] = [];
	for (const interest of interests) {
		const id = typeof interest === 'string' ? interest : interest.id;
		for (const careerId of mappedInterestIds[id] ?? []) {
			if (!ids.has(careerId)) {
				ids.add(careerId);
				ordered.push(careerId);
			}
		}
	}
	return ordered
		.map((id) => assessmentCareers.find((career) => career.id === id))
		.filter((career): career is AssessmentCareer => Boolean(career));
}

export function validateAssessmentConfig(config: AssessmentConfig = assessmentConfig): void {
	const idPattern = /^[a-zA-Z0-9][a-zA-Z0-9_-]*$/;
	if (
		config.dimensions.length !== 28 ||
		new Set(config.dimensions.map((item) => item.id)).size !== 28
	)
		throw new Error('Cấu hình phải có đúng 28 nhóm DESMAP duy nhất.');
	if (config.careers.length !== 5 || new Set(config.careers.map((item) => item.id)).size !== 5)
		throw new Error('Cấu hình phải có đúng 5 nghề duy nhất.');
	const ids = new Set(config.dimensions.map((item) => item.id));
	for (const dimension of config.dimensions) {
		if (!idPattern.test(dimension.id) || !dimension.name.trim() || !dimension.description.trim())
			throw new Error(`Nhóm DESMAP ${dimension.id} không hợp lệ.`);
	}
	for (const career of config.careers) {
		if (!idPattern.test(career.id) || !career.name.trim() || !career.description.trim())
			throw new Error(`Nghề ${career.id} không hợp lệ.`);
		const weightKeys = Object.keys(career.weights);
		if (weightKeys.length !== 28 || weightKeys.some((id) => !ids.has(id as GroupedDimensionId)))
			throw new Error(`Cấu hình trọng số của ${career.id} chưa đủ 28 nhóm.`);
		for (const dimension of config.dimensions) {
			const weight = career.weights[dimension.id];
			if (!Number.isInteger(weight) || weight < 1 || weight > 5 || !ids.has(dimension.id))
				throw new Error(`Trọng số không hợp lệ của ${career.id}.`);
		}
	}
	for (const [interestId, careerIds] of Object.entries(config.interestToCareers)) {
		if (
			!interestId ||
			!careerInterestOptions.some((interest) => interest.id === interestId) ||
			careerIds.length === 0 ||
			careerIds.some((id) => !config.careers.some((career) => career.id === id))
		)
			throw new Error(`Ánh xạ nghề của sở thích ${interestId} không hợp lệ.`);
	}
	if (
		Object.keys(config.interestToCareers).length !== careerInterestOptions.length ||
		careerInterestOptions.some((interest) => !(interest.id in config.interestToCareers))
	)
		throw new Error('Cấu hình ánh xạ sở thích chưa đầy đủ.');
}

validateAssessmentConfig();

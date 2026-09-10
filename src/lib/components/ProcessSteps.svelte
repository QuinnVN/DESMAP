<script lang="ts">
const steps = [
	{
		number: "01",
		title: "Sở thích",
		copy: "Chọn nghề nghiệp bạn muốn khám phá.",
		icon: "/desmap/landing-02.png",
		delay: "[animation-delay:0ms]",
	},
	{
		number: "02",
		title: "Bài test DESMAP",
		copy: "Khám phá đặc điểm và điểm mạnh của bạn.",
		icon: "/desmap/landing-03.png",
		delay: "[animation-delay:90ms]",
	},
	{
		number: "03",
		title: "Trải nghiệm VR",
		copy: "Trải nghiệm mô phỏng nghề nghiệp thực tế.",
		icon: "/desmap/landing-04.png",
		delay: "[animation-delay:180ms]",
	},
	{
		number: "04",
		title: "Hồ sơ cuối cùng",
		copy: "Nhận kết quả và lộ trình phát triển của bạn.",
		icon: "/desmap/landing-05.png",
		delay: "[animation-delay:270ms]",
	},
];
</script>

<section
	class="relative overflow-hidden py-[clamp(4.5rem,9vw,9rem)] before:pointer-events-none before:absolute before:inset-x-0 before:top-[20%] before:h-72 before:bg-[radial-gradient(ellipse_at_48%_40%,rgb(0_95_255_/.11),transparent_65%)]"
	id="how-it-works"
	aria-labelledby="process-heading"
>
	<div
		class="mx-auto w-[min(100%_-_2rem,90rem)] min-[761px]:w-[min(100%_-_4rem,90rem)]"
	>
		<div
			class="flex flex-col items-start justify-between gap-2 border-b border-line pb-[1.4rem] font-mono text-[.69rem] tracking-[.13em] text-lime uppercase min-[761px]:flex-row min-[761px]:items-center [&>span:last-child]:text-muted"
		>
			<span>Cách hoạt động</span
			><span>Từ khám phá bản thân đến trải nghiệm thực tế</span>
		</div>
		<div
			class="block gap-8 py-[2.3rem] min-[761px]:flex min-[761px]:items-end min-[761px]:justify-between min-[761px]:py-[3.1rem_3.7rem]"
		>
			<h2
				class="m-0 max-w-[31rem] text-[clamp(2.5rem,6vw,5.2rem)] leading-[.9] font-[430] tracking-[-.08em] text-text uppercase"
				id="process-heading"
			>
				Lộ trình rõ ràng hơn<br>
				<span class="text-lime">cho bước đi tiếp theo.</span>
			</h2>
			<p
				class="mt-[1.3rem] mb-0 max-w-72 leading-normal text-muted min-[761px]:mt-0"
			>
				Bốn bước trọng tâm biến những câu hỏi về tương lai thành hồ sơ để bạn
				hành động.
			</p>
		</div>
		<div class="grid grid-cols-2 border-t border-line min-[761px]:grid-cols-4">
			{#each steps as step (step.number)}
				<article
					class={`group min-h-84 animate-rise-in border-r border-line px-[.85rem] pt-[1.4rem] motion-reduce:animate-none min-[471px]:min-h-[21rem] min-[471px]:px-[1.2rem] min-[761px]:min-h-[25rem] [&:first-child]:border-l [&:nth-child(2)]:border-r-0 min-[761px]:[&:nth-child(2)]:border-r [&:nth-child(3)]:border-t min-[761px]:[&:nth-child(3)]:border-t-0 [&:nth-child(4)]:border-t [&:nth-child(4)]:border-r-0 min-[761px]:[&:nth-child(4)]:border-t-0 ${step.delay}`}
				>
					<div class="flex items-center justify-between">
						<span
							class="text-[2.4rem] leading-none font-[360] tracking-[-.09em] text-lime min-[471px]:text-[3.1rem]"
							>{step.number}</span
						><span
							class="text-[1.3rem] text-blue transition group-hover:translate-x-[.2rem] group-hover:-translate-y-[.2rem] group-hover:text-lime"
							aria-hidden="true"
							>↗</span
						>
					</div>
					<div
						class="mx-auto my-[2rem_1.8rem] h-[4.3rem] w-[4.3rem] rounded-full border border-blue bg-[#050c1d] p-3 shadow-[0_0_1.1rem_rgb(7_97_255_/.56),inset_0_0_.8rem_rgb(32_97_255_/.4)] group-hover:border-lime group-hover:shadow-[0_0_1.6rem_rgb(188_255_99_/.4),inset_0_0_.8rem_rgb(188_255_99_/.2)] min-[471px]:my-[3rem_2.3rem] min-[471px]:h-20 min-[471px]:w-20"
					>
						<img
							class="h-full w-full object-contain drop-shadow-[0_0_.4rem_#3a87ff]"
							src={step.icon}
							alt=""
							loading="lazy"
						>
					</div>
					<h3 class="m-0 font-mono text-[.9rem] tracking-[.07em] uppercase">
						{step.title}
					</h3>
					<p
						class="mt-[.8rem] mb-0 max-w-48 text-[.8rem] leading-[1.45] text-muted min-[471px]:text-[.9rem]"
					>
						{step.copy}
					</p>
				</article>
			{/each}
		</div>
	</div>
</section>

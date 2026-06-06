using HomeschoolManager.Domain.Curriculum;

namespace HomeschoolManager.Application.Courses;

public static class DefaultCoursePacks
{
    public const string MichiganCollegeReadyPackId = "mi-college-recognizable-core-v1";
    private const string DefaultInstructionalMethods =
        "Explicit instruction with guided practice, discussion, independent reading or problem work, applied projects, and parent feedback. Lessons begin with clear goals, move through modeled examples, and end with student practice or reflection.";
    private const string DefaultAssessmentMethods =
        "Ongoing formative checks, reviewed assignments, discussion or conference notes, quizzes or problem sets where appropriate, project or performance evidence, and a final portfolio review or summative evaluation.";
    private const string DefaultGradingBasis =
        "Mastery-aligned letter grade using parent-reviewed evidence. Suggested weighting: 40% assignments/practice, 30% projects or performance evidence, 20% quizzes/tests or demonstrations, and 10% participation/reflection.";

    public static IReadOnlyList<CoursePackDefinition> All { get; } =
    [
        new CoursePackDefinition(
            MichiganCollegeReadyPackId,
            "Michigan college-recognizable high school core",
            "A transcript-friendly planning starter based on common Michigan Merit Curriculum credit categories and Michigan homeschool subject areas.",
            "Michigan",
            [
                FullYear("ela-12", "English Language Arts 12", ["English Language Arts", "Reading", "Literature", "Writing", "English Grammar", "Spelling"], 1,
                    "Senior English emphasizing literature, composition, grammar, vocabulary, and revision.",
                    [Map("MMC Reference", "English Language Arts", CoverageLevel.Primary), Map("MDE Summary", "English Language Arts", CoverageLevel.Primary), Map("Statutory", "Reading", CoverageLevel.Primary), Map("Statutory", "Literature", CoverageLevel.Primary), Map("Statutory", "Writing", CoverageLevel.Primary), Map("Statutory", "English Grammar", CoverageLevel.Secondary), Map("Statutory", "Spelling", CoverageLevel.Supporting)]),
                Choice("math-12", "Senior Mathematics", "precalculus",
                    [
                        MathOption("math-12", "Math 12", "A senior mathematics course reviewing algebra, functions, modeling, data, and practical quantitative reasoning."),
                        MathOption("pre-algebra", "Pre-Algebra", "A foundations course strengthening arithmetic, proportional reasoning, signed numbers, variables, and problem solving."),
                        MathOption("algebra-i", "Algebra I", "A first algebra course covering linear relationships, equations, inequalities, functions, exponents, and introductory data analysis."),
                        MathOption("geometry", "Geometry", "A geometry course covering proof, congruence, similarity, coordinate geometry, measurement, transformations, and geometric reasoning."),
                        MathOption("algebra-ii", "Algebra II", "An advanced algebra course covering functions, systems, polynomials, rational expressions, radicals, exponentials, logarithms, and modeling."),
                        MathOption("trigonometry", "Trigonometry", "A course covering right-triangle and circular trigonometry, identities, graphs, inverse functions, vectors, and applications."),
                        MathOption("precalculus", "Precalculus", "A college-preparatory senior mathematics course covering advanced functions, trigonometry, analytic geometry, sequences, and readiness for calculus."),
                        MathOption("calculus-i", "Calculus I", "An introductory calculus course covering limits, derivatives, applications of differentiation, integrals, and the fundamental theorem of calculus."),
                        MathOption("calculus-ii", "Calculus II", "A second calculus course covering integration techniques, applications, sequences, series, and additional analytic methods."),
                        MathOption("calculus-iii", "Calculus III", "A multivariable calculus course covering vectors, partial derivatives, multiple integrals, and three-dimensional analytic geometry.")
                    ]),
                Choice("science", "Science", "physics",
                    [
                        ScienceOption("physics", "Physics", "A laboratory or inquiry-oriented physics course covering motion, forces, energy, waves, electricity, magnetism, and scientific modeling."),
                        ScienceOption("environmental-science", "Environmental Science", "A science course covering ecosystems, natural resources, human environmental impact, conservation, and evidence-based environmental analysis."),
                        ScienceOption("anatomy-physiology", "Anatomy and Physiology", "A life science course covering human body systems, structure and function, health connections, and laboratory or applied investigations."),
                        ScienceOption("chemistry", "Chemistry", "A laboratory or inquiry-oriented chemistry course covering matter, atomic structure, bonding, reactions, stoichiometry, solutions, and chemical reasoning."),
                        ScienceOption("advanced-biology", "Advanced Biology", "An upper-level biology course covering genetics, cells, evolution, ecology, anatomy, or other advanced life science topics."),
                        ScienceOption("earth-space-science", "Earth and Space Science", "A science course covering geology, meteorology, astronomy, earth systems, natural processes, and scientific evidence."),
                        ScienceOption("forensic-science", "Forensic Science", "An applied science course using biology, chemistry, physics, and evidence analysis in case-based investigations."),
                        ScienceOption("astronomy", "Astronomy", "A physical science course covering the solar system, stars, galaxies, cosmology, observation, and scientific models of space.")
                    ]),
                Choice("social-studies", "Social Studies", "government-economics",
                    [
                        Option("government-economics", "Government and Economics", ["Social Studies", "Civics", "Economics"], CourseDuration.TwoSemesters, 1,
                            "A senior social studies course combining American government, citizenship, civic participation, economic reasoning, and personal or applied economics.",
                            [Map("MMC Reference", "Social Studies", CoverageLevel.Primary), Map("MDE Summary", "Social Studies", CoverageLevel.Primary), Map("Statutory", "Civics", CoverageLevel.Primary)]),
                        Option("government-civics", "Government and Civics", ["Social Studies", "Civics"], CourseDuration.OneSemester, 0.5m,
                            "A one-semester government and civics course covering constitutional principles, citizenship, rights, responsibilities, and civic participation.",
                            [Map("MMC Reference", "Social Studies", CoverageLevel.Secondary), Map("MDE Summary", "Social Studies", CoverageLevel.Secondary), Map("Statutory", "Civics", CoverageLevel.Primary)]),
                        Option("economics", "Economics", ["Social Studies", "Economics"], CourseDuration.OneSemester, 0.5m,
                            "A one-semester economics course covering personal, microeconomic, macroeconomic, or applied economic concepts.",
                            [Map("MMC Reference", "Social Studies", CoverageLevel.Secondary)]),
                        Option("us-history", "U.S. History", ["Social Studies", "History"], CourseDuration.TwoSemesters, 1,
                            "A United States history course covering major eras, historical evidence, civic context, and continuity and change over time.",
                            [Map("MMC Reference", "Social Studies", CoverageLevel.Primary), Map("MDE Summary", "Social Studies", CoverageLevel.Primary), Map("Statutory", "History", CoverageLevel.Primary)]),
                        Option("world-history", "World History", ["Social Studies", "History"], CourseDuration.TwoSemesters, 1,
                            "A world history course covering global eras, geography, culture, conflict, exchange, and historical inquiry.",
                            [Map("MMC Reference", "Social Studies", CoverageLevel.Primary), Map("MDE Summary", "Social Studies", CoverageLevel.Primary), Map("Statutory", "History", CoverageLevel.Primary)]),
                        Option("psychology", "Psychology", ["Social Studies"], CourseDuration.OneSemester, 0.5m,
                            "A social science elective covering behavior, cognition, development, research methods, and applications of psychological concepts.",
                            [Map("MMC Reference", "Social Studies", CoverageLevel.Supporting)]),
                        Option("sociology", "Sociology", ["Social Studies"], CourseDuration.OneSemester, 0.5m,
                            "A social science elective covering culture, institutions, groups, social change, and sociological perspectives.",
                            [Map("MMC Reference", "Social Studies", CoverageLevel.Supporting)])
                    ]),
                Semester("personal-finance", "Personal Finance", ["Personal Finance", "Mathematics"], 0.5m,
                    "A one-semester personal finance course covering budgeting, banking, credit, insurance, taxes, and long-term planning.",
                    [Map("MMC Reference", "Personal Finance", CoverageLevel.Primary), Map("Statutory", "Mathematics", CoverageLevel.Supporting)]),
                Choice("world-language", "World Language", "spanish",
                    [
                        WorldLanguageOption("spanish", "Spanish", "A world language course developing Spanish listening, speaking, reading, writing, vocabulary, grammar, and cultural understanding."),
                        WorldLanguageOption("french", "French", "A world language course developing French listening, speaking, reading, writing, vocabulary, grammar, and cultural understanding."),
                        WorldLanguageOption("american-sign-language", "American Sign Language", "A world language course developing receptive and expressive ASL skills, visual communication, Deaf culture, and practical signed interaction."),
                        WorldLanguageOption("german", "German", "A world language course developing German listening, speaking, reading, writing, vocabulary, grammar, and cultural understanding."),
                        WorldLanguageOption("chinese-mandarin", "Chinese Mandarin", "A world language course developing Mandarin listening, speaking, reading, writing, character familiarity, vocabulary, and cultural understanding."),
                        WorldLanguageOption("latin", "Latin", "A classical language course developing Latin vocabulary, grammar, translation, Roman culture, and connections to English vocabulary and literature."),
                        WorldLanguageOption("japanese", "Japanese", "A world language course developing Japanese listening, speaking, reading, writing systems, vocabulary, grammar, and cultural understanding."),
                        WorldLanguageOption("arabic", "Arabic", "A world language course developing Arabic listening, speaking, reading, writing, vocabulary, grammar, and cultural understanding."),
                        WorldLanguageOption("italian", "Italian", "A world language course developing Italian listening, speaking, reading, writing, vocabulary, grammar, and cultural understanding.")
                    ]),
                Semester("pe-health", "Physical Education and Health", ["Physical Education", "Health"], 0.5m,
                    "A one-semester course or integrated learning record for health concepts and physical education activity.",
                    [Map("MMC Reference", "Physical Education and Health", CoverageLevel.Primary)]),
                Choice("visual-performing-applied-arts", "Visual, Performing, or Applied Arts", "studio-art",
                    [
                        ArtsOption("studio-art", "Studio Art", "A visual arts course emphasizing creative process, design principles, media exploration, critique, and a portfolio of finished work."),
                        ArtsOption("drawing-painting", "Drawing and Painting", "A visual arts course covering drawing, painting, composition, observation, technique, and creative expression."),
                        ArtsOption("photography-digital-media", "Photography and Digital Media", "An arts course covering image composition, digital tools, visual communication, editing, and portfolio development."),
                        ArtsOption("graphic-design", "Graphic Design", "An applied arts course covering design principles, typography, layout, digital production, and visual problem solving."),
                        ArtsOption("theater", "Theater", "A performing arts course covering acting, script study, production, performance, and reflection."),
                        ArtsOption("choir-vocal-music", "Choir or Vocal Music", "A performing arts course covering vocal technique, repertoire, music literacy, rehearsal, and performance."),
                        ArtsOption("band-instrumental-music", "Band or Instrumental Music", "A performing arts course covering instrumental technique, music literacy, ensemble rehearsal, and performance."),
                        ArtsOption("ceramics", "Ceramics", "A visual arts course covering hand-building, wheel techniques, glazing, critique, and finished ceramic work."),
                        ArtsOption("applied-design", "Applied Design", "An applied arts course covering design thinking, materials, function, aesthetics, production, and project documentation.")
                    ]),
                Choice("online-learning", "Online Learning Experience", "experiential-capstone",
                    [
                        ElectiveOption("experiential-capstone", "Experiential Capstone", "A customizable capstone experience integrating academic skills, independent inquiry, applied work, reflection, and a final product or portfolio."),
                        ElectiveOption("career-exploration", "Career Exploration", "An elective course exploring career pathways, workplace skills, interviews, planning, and evidence from practical learning experiences."),
                        ElectiveOption("computer-science", "Computer Science", "An elective course covering programming concepts, problem solving, algorithms, digital systems, and project-based computing work."),
                        ElectiveOption("creative-writing", "Creative Writing", "An elective course covering fiction, nonfiction, poetry, revision, publication, critique, and a portfolio of original writing."),
                        ElectiveOption("entrepreneurship", "Entrepreneurship", "An elective course covering business ideas, customers, budgeting, marketing, operations, and a practical venture or project plan."),
                        ElectiveOption("independent-research", "Independent Research", "An elective course centered on a parent-approved research question, source evaluation, writing, presentation, and documented findings."),
                        ElectiveOption("college-readiness", "College and Career Readiness", "An elective course covering planning, applications, study systems, communication, financial preparation, and transition skills."),
                        ElectiveOption("work-based-learning", "Work-Based Learning", "An elective course documenting supervised work, employability skills, applied learning, reflection, and parent-evaluated evidence.")
                    ])
            ])
    ];

    private static CourseTemplateDefinition FullYear(string id, string title, IReadOnlyList<string> subjects, decimal credits, string description, IReadOnlyList<CourseTemplateRequirementMapping> mappings)
    {
        return Template(id, title, subjects, CourseDuration.TwoSemesters, credits, description, mappings);
    }

    private static CourseTemplateDefinition Semester(string id, string title, IReadOnlyList<string> subjects, decimal credits, string description, IReadOnlyList<CourseTemplateRequirementMapping> mappings)
    {
        return Template(id, title, subjects, CourseDuration.OneSemester, credits, description, mappings);
    }

    private static CourseTemplateDefinition Template(string id, string title, IReadOnlyList<string> subjects, CourseDuration duration, decimal credits, string description, IReadOnlyList<CourseTemplateRequirementMapping> mappings)
    {
        var option = Option(id, title, subjects, duration, credits, description, mappings);
        return new CourseTemplateDefinition(
            id,
            title,
            subjects,
            duration,
            credits,
            option.Description,
            CurriculumPlan.Empty,
            mappings,
            id,
            [option]);
    }

    private static CourseTemplateDefinition Choice(
        string id,
        string title,
        string defaultOptionId,
        IReadOnlyList<CourseTemplateOptionDefinition> options)
    {
        var defaultOption = options.First(option => string.Equals(option.OptionId, defaultOptionId, StringComparison.OrdinalIgnoreCase));
        return new CourseTemplateDefinition(
            id,
            title,
            defaultOption.SubjectAreas,
            defaultOption.Duration,
            defaultOption.PlannedCreditValue,
            defaultOption.Description,
            defaultOption.CurriculumPlan,
            defaultOption.RequirementMappings,
            defaultOptionId,
            options);
    }

    private static CourseTemplateOptionDefinition MathOption(string id, string title, string description)
    {
        return Option(
            id,
            title,
            ["Mathematics"],
            CourseDuration.TwoSemesters,
            1,
            description,
            [Map("MMC Reference", "Mathematics", CoverageLevel.Primary), Map("MDE Summary", "Mathematics", CoverageLevel.Primary), Map("Statutory", "Mathematics", CoverageLevel.Primary)]);
    }

    private static CourseTemplateOptionDefinition ScienceOption(string id, string title, string description)
    {
        return Option(
            id,
            title,
            ["Science"],
            CourseDuration.TwoSemesters,
            1,
            description,
            [Map("MMC Reference", "Science", CoverageLevel.Primary), Map("MDE Summary", "Science", CoverageLevel.Primary), Map("Statutory", "Science", CoverageLevel.Primary)]);
    }

    private static CourseTemplateOptionDefinition ArtsOption(string id, string title, string description)
    {
        return Option(
            id,
            title,
            ["Visual, Performing, and Applied Arts"],
            CourseDuration.OneSemester,
            0.5m,
            description,
            [Map("MMC Reference", "Visual, Performing, and Applied Arts", CoverageLevel.Primary)]);
    }

    private static CourseTemplateOptionDefinition WorldLanguageOption(string id, string title, string description)
    {
        return Option(
            id,
            title,
            ["World Language"],
            CourseDuration.TwoSemesters,
            1,
            description,
            [Map("MMC Reference", "World Language", CoverageLevel.Primary)]);
    }

    private static CourseTemplateOptionDefinition ElectiveOption(string id, string title, string description)
    {
        return Option(
            id,
            title,
            ["Online Learning", "Elective"],
            CourseDuration.OneSemester,
            0.5m,
            description,
            [Map("MMC Reference", "Online Learning Experience", CoverageLevel.Primary)]);
    }

    private static CourseTemplateOptionDefinition Option(
        string id,
        string title,
        IReadOnlyList<string> subjects,
        CourseDuration duration,
        decimal credits,
        string description,
        IReadOnlyList<CourseTemplateRequirementMapping> mappings)
    {
        return new CourseTemplateOptionDefinition(
            id,
            title,
            subjects,
            duration,
            credits,
            new CourseDescription(
                description,
                DefaultInstructionalMethods,
                MajorTopicsFor(title),
                TextsAndResourcesFor(title),
                DefaultAssessmentMethods,
                DefaultGradingBasis),
            CurriculumPlanFor(title, subjects, duration),
            mappings);
    }

    private static CourseTemplateRequirementMapping Map(string view, string name, CoverageLevel level)
    {
        return new CourseTemplateRequirementMapping(view, name, level, "Imported from default course pack.");
    }

    private static CurriculumPlan CurriculumPlanFor(string title, IReadOnlyList<string> subjects, CourseDuration duration)
    {
        var subjectText = string.Join(", ", subjects);
        return new CurriculumPlan(
            $"Build a transcript-ready understanding of {title} through clear instruction, documented practice, applied work, and parent-reviewed evidence.",
            $"Explain major concepts in {title}; apply course skills in written, oral, practical, or problem-based work; use appropriate vocabulary and resources; and produce evidence suitable for course records.",
            TextsAndResourcesFor(title),
            duration == CourseDuration.TwoSemesters
                ? $"Semester 1: foundations, core vocabulary, guided practice, and early projects. Semester 2: advanced topics, independent application, review, and a final portfolio or capstone evidence set for {subjectText}."
                : $"Weeks 1-4: foundations and vocabulary. Weeks 5-10: guided practice and applied work. Weeks 11-16: independent application, review, and final evidence set for {subjectText}.",
            "Imported pack plan. Parent should customize resources, pacing, assignments, assessment evidence, and grading notes to match the actual course.");
    }

    private static string MajorTopicsFor(string title)
    {
        return title switch
        {
            "English Language Arts 12" => "Close reading; literary analysis; composition; research writing; grammar and usage; vocabulary; revision; presentation and discussion.",
            "Math 12" => "Algebra review; functions; quantitative reasoning; modeling; data interpretation; financial and practical applications; problem-solving communication.",
            "Pre-Algebra" => "Number operations; ratios and proportions; expressions; equations; inequalities; graphing; geometry foundations; word problems.",
            "Algebra I" => "Linear equations; inequalities; functions; systems; exponents; polynomials; factoring foundations; data analysis; modeling.",
            "Geometry" => "Proof; congruence; similarity; right triangles; circles; coordinate geometry; transformations; area and volume; geometric modeling.",
            "Algebra II" => "Functions; systems; polynomials; rational expressions; radicals; complex numbers; exponential and logarithmic models; sequences and series.",
            "Trigonometry" => "Right-triangle trigonometry; unit circle; graphs; identities; inverse functions; vectors; applications and modeling.",
            "Precalculus" => "Advanced functions; trigonometry; analytic geometry; sequences; limits preview; modeling; readiness for calculus.",
            "Calculus I" => "Limits; derivatives; derivative applications; integrals; fundamental theorem of calculus; modeling change.",
            "Calculus II" => "Integration techniques; applications of integration; differential equations preview; sequences; series; parametric and polar topics.",
            "Calculus III" => "Vectors; three-dimensional space; partial derivatives; multiple integrals; vector fields; multivariable applications.",
            "Physics" => "Motion; forces; energy; momentum; waves; electricity; magnetism; scientific models; lab or simulation evidence.",
            "Environmental Science" => "Ecosystems; biodiversity; resources; climate and weather; pollution; conservation; human impact; environmental decision-making.",
            "Anatomy and Physiology" => "Body organization; tissues; skeletal, muscular, nervous, cardiovascular, respiratory, digestive, and endocrine systems; health applications.",
            "Chemistry" => "Matter; atomic structure; periodic trends; bonding; reactions; stoichiometry; solutions; acids and bases; laboratory reasoning.",
            "Advanced Biology" => "Cells; genetics; evolution; ecology; anatomy or physiology topics; biotechnology; research literacy; lab or field evidence.",
            "Earth and Space Science" => "Geology; earth systems; weather and climate; astronomy; natural hazards; maps and models; scientific evidence.",
            "Forensic Science" => "Evidence collection; observation; biology applications; chemistry applications; physics applications; case analysis; scientific reporting.",
            "Astronomy" => "Observation; solar system; stars; galaxies; cosmology; space exploration; light and spectra; scientific models.",
            "Government and Economics" => "Constitutional principles; branches of government; citizenship; civil rights; elections; economic decision-making; markets; personal economics.",
            "Government and Civics" => "Constitutional principles; citizenship; rights and responsibilities; civic participation; public policy; government institutions.",
            "Economics" => "Scarcity; incentives; markets; supply and demand; personal finance connections; macroeconomic indicators; economic decision-making.",
            "U.S. History" => "Founding; constitutional development; reform; conflict; industrialization; civil rights; modern America; historical evidence.",
            "World History" => "Ancient and classical societies; global exchange; belief systems; revolutions; conflict; globalization; historical inquiry.",
            "Psychology" => "Research methods; brain and behavior; development; learning; cognition; personality; social psychology; mental health literacy.",
            "Sociology" => "Culture; groups; socialization; institutions; inequality; social change; research methods; community analysis.",
            "Personal Finance" => "Budgeting; saving; banking; credit; debt; insurance; taxes; investing; career income; long-term planning.",
            "Physical Education and Health" => "Fitness planning; physical activity; nutrition; mental health; safety; substance awareness; personal wellness habits.",
            "Experiential Capstone" => "Project planning; independent inquiry; applied skills; documentation; reflection; revision; final product or portfolio presentation.",
            _ when title.Contains("Spanish", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("French", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("German", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Chinese", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Japanese", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Arabic", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Italian", StringComparison.OrdinalIgnoreCase) => "Listening; speaking; reading; writing; vocabulary; grammar; culture; practical communication; language-learning reflection.",
            "American Sign Language" => "Receptive signing; expressive signing; visual grammar; fingerspelling; Deaf culture; conversational practice; signed presentation.",
            "Latin" => "Vocabulary; grammar; translation; Roman culture; classical roots; reading passages; connections to English and literature.",
            _ when title.Contains("Art", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Drawing", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Painting", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Photography", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Design", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Ceramics", StringComparison.OrdinalIgnoreCase) => "Creative process; design principles; media techniques; critique; artist study; portfolio development; final project evidence.",
            _ when title.Contains("Theater", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Choir", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Music", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Band", StringComparison.OrdinalIgnoreCase) => "Technique; repertoire or script study; rehearsal; performance; critique; reflection; portfolio or performance evidence.",
            _ => "Course vocabulary; core concepts; guided practice; applied assignments; discussion; independent work; final portfolio evidence."
        };
    }

    private static string TextsAndResourcesFor(string title)
    {
        return title switch
        {
            "English Language Arts 12" => "CommonLit high school texts; Project Gutenberg public-domain literature; Purdue OWL writing resources; parent-selected novels, essays, speeches, and poetry.",
            "Math 12" => "Khan Academy high school math; CK-12 math resources; OpenStax Algebra and Statistics chapters as needed; parent-created application problems.",
            "Pre-Algebra" => "Khan Academy pre-algebra; CK-12 Pre-Algebra; OpenStax Prealgebra; parent-created practice and real-life problems.",
            "Algebra I" => "Khan Academy Algebra 1; CK-12 Algebra; OpenStax Elementary Algebra or Intermediate Algebra chapters; graphing calculator or Desmos activities.",
            "Geometry" => "Khan Academy Geometry; CK-12 Geometry; Illustrative Mathematics geometry tasks; Desmos geometry and graphing activities.",
            "Algebra II" => "Khan Academy Algebra 2; CK-12 Algebra II; OpenStax Intermediate Algebra or College Algebra chapters; Desmos activities.",
            "Trigonometry" => "Khan Academy Trigonometry; CK-12 Trigonometry; OpenStax Precalculus trigonometry chapters; Desmos graphing activities.",
            "Precalculus" => "OpenStax Precalculus; Khan Academy Precalculus; CK-12 Precalculus; Desmos graphing activities.",
            "Calculus I" => "OpenStax Calculus Volume 1; Khan Academy Calculus; MIT OpenCourseWare single-variable calculus support; Desmos or graphing tools.",
            "Calculus II" => "OpenStax Calculus Volume 2; Khan Academy Calculus; MIT OpenCourseWare single-variable calculus support; graphing and symbolic tools as appropriate.",
            "Calculus III" => "OpenStax Calculus Volume 3; MIT OpenCourseWare multivariable calculus support; 3D graphing or visualization tools.",
            "Physics" => "OpenStax Physics or College Physics; PhET simulations; Khan Academy Physics; home lab demonstrations or documented investigations.",
            "Environmental Science" => "CK-12 Environmental Science; EPA educational resources; NOAA climate resources; local field observations and data collection.",
            "Anatomy and Physiology" => "OpenStax Anatomy and Physiology; visible body or anatomy diagrams; Khan Academy health and medicine; parent-approved labs or models.",
            "Chemistry" => "OpenStax Chemistry 2e or Chemistry: Atoms First; PhET chemistry simulations; Khan Academy Chemistry; safe home lab demonstrations.",
            "Advanced Biology" => "OpenStax Biology 2e; HHMI BioInteractive; Khan Academy Biology; microscope, field, or model-based investigations where available.",
            "Earth and Space Science" => "CK-12 Earth Science; NASA educational resources; NOAA weather and climate resources; sky observation and local geology records.",
            "Forensic Science" => "Open educational forensic science readings; case-study packets; safe observation, measurement, chemistry, and biology demonstrations.",
            "Astronomy" => "OpenStax Astronomy; NASA educational resources; sky observation logs; planetarium or observatory resources where available.",
            "Government and Economics" => "iCivics; National Constitution Center; Federalist Papers excerpts; OpenStax American Government; EconEd and CFPB resources.",
            "Government and Civics" => "iCivics; National Constitution Center; Federalist Papers excerpts; OpenStax American Government; local/state government sources.",
            "Economics" => "EconEd; OpenStax Principles of Economics selected chapters; Khan Academy economics; CFPB personal finance resources.",
            "U.S. History" => "OpenStax U.S. History; Library of Congress primary sources; National Archives DocsTeach; Gilder Lehrman resources.",
            "World History" => "World History for Us All; OpenStax World History selected chapters; OER Project resources; primary source excerpts.",
            "Psychology" => "OpenStax Psychology 2e; APA high school psychology resources; teacher-selected case studies and reflection prompts.",
            "Sociology" => "OpenStax Introduction to Sociology 3e; Census and community data; teacher-selected articles and observation activities.",
            "Personal Finance" => "CFPB Money Topics and youth financial education; FDIC Money Smart; Next Gen Personal Finance; practical household budgeting exercises.",
            "Physical Education and Health" => "CDC health education resources; MedlinePlus; SHAPE America guidance; fitness logs and parent-approved activity plans.",
            "Experiential Capstone" => "Parent-selected project resources; interviews or mentorship notes; research sources; project log; portfolio artifacts; final presentation materials.",
            _ when title.Contains("Computer Science", StringComparison.OrdinalIgnoreCase) => "Code.org; freeCodeCamp; Khan Academy computing; project repository or notebook; parent-selected programming references.",
            _ when title.Contains("Career", StringComparison.OrdinalIgnoreCase) => "Bureau of Labor Statistics Occupational Outlook Handbook; CareerOneStop; interview notes; resume and planning templates.",
            _ when title.Contains("Creative Writing", StringComparison.OrdinalIgnoreCase) => "Writing prompts; mentor texts; Purdue OWL; NaNoWriMo Young Writers resources; revision workshop notes.",
            _ when title.Contains("Entrepreneurship", StringComparison.OrdinalIgnoreCase) => "SBA and SCORE resources; business model canvas; budgeting worksheets; customer discovery notes; pitch materials.",
            _ when title.Contains("Independent Research", StringComparison.OrdinalIgnoreCase) => "Library databases or public sources; citation guide; research notebook; outline drafts; final paper or presentation.",
            _ when title.Contains("College", StringComparison.OrdinalIgnoreCase) => "College Board BigFuture; Federal Student Aid resources; application checklists; study planning templates.",
            _ when title.Contains("Work-Based", StringComparison.OrdinalIgnoreCase) => "Supervisor or mentor feedback; work logs; employability skill rubrics; safety and workplace policy materials.",
            _ when title.Contains("Spanish", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("French", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("German", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Chinese", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Japanese", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Arabic", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Italian", StringComparison.OrdinalIgnoreCase) ||
                title == "American Sign Language" ||
                title == "Latin" => "ACTFL Can-Do Statements; parent-selected language text or online course; conversation practice; vocabulary notebook; cultural readings and media.",
            _ when title.Contains("Art", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Drawing", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Painting", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Photography", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Design", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Ceramics", StringComparison.OrdinalIgnoreCase) => "Khan Academy art resources; museum education resources; artist examples; sketchbook or process journal; portfolio artifacts.",
            _ when title.Contains("Theater", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Choir", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Music", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Band", StringComparison.OrdinalIgnoreCase) => "Parent-selected repertoire or script; performance recordings; music theory or theater resources; rehearsal log; critique notes.",
            _ => "Parent-selected spine text or course platform; open educational resources; notebooks; project evidence; portfolio artifacts."
        };
    }
}

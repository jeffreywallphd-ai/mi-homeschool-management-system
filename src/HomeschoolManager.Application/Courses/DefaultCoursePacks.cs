using HomeschoolManager.Domain.Curriculum;

namespace HomeschoolManager.Application.Courses;

public static class DefaultCoursePacks
{
    public const string MichiganCollegeReadyPackId = "mi-college-recognizable-core-v1";
    private const string DefaultInstructionalMethods =
        "Hybrid instructional plan combining explicit instruction, guided practice, discussion, independent reading or problem work, applied projects, and parent feedback. Lessons begin with clear goals, move through modeled examples, and end with student practice or reflection.";
    private const string DefaultAssessmentMethods =
        "Hybrid assessment plan combining ongoing formative checks, reviewed assignments, discussion or conference notes, quizzes or problem sets where appropriate, project or performance evidence, and a final portfolio review or summative evaluation.";
    private const string DefaultGradingBasis =
        "Hybrid grading basis using a mastery-aligned letter grade from parent-reviewed evidence. Suggested weighting: 40% assignments/practice, 30% projects or performance evidence, 20% quizzes/tests or demonstrations, and 10% participation/reflection.";

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
                    [Map("Statutory", "Reading", CoverageLevel.Primary), Map("Statutory", "Literature", CoverageLevel.Primary), Map("Statutory", "Writing", CoverageLevel.Primary), Map("Statutory", "English Grammar", CoverageLevel.Secondary), Map("Statutory", "Spelling", CoverageLevel.Supporting)]),
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
                            [Map("Statutory", "Civics", CoverageLevel.Primary)]),
                        Option("government-civics", "Government and Civics", ["Social Studies", "Civics"], CourseDuration.OneSemester, 0.5m,
                            "A one-semester government and civics course covering constitutional principles, citizenship, rights, responsibilities, and civic participation.",
                            [Map("Statutory", "Civics", CoverageLevel.Primary)]),
                        Option("economics", "Economics", ["Social Studies", "Economics"], CourseDuration.OneSemester, 0.5m,
                            "A one-semester economics course covering personal, microeconomic, macroeconomic, or applied economic concepts.",
                            []),
                        Option("us-history", "U.S. History", ["Social Studies", "History"], CourseDuration.TwoSemesters, 1,
                            "A United States history course covering major eras, historical evidence, civic context, and continuity and change over time.",
                            [Map("Statutory", "History", CoverageLevel.Primary)]),
                        Option("world-history", "World History", ["Social Studies", "History"], CourseDuration.TwoSemesters, 1,
                            "A world history course covering global eras, geography, culture, conflict, exchange, and historical inquiry.",
                            [Map("Statutory", "History", CoverageLevel.Primary)]),
                        Option("psychology", "Psychology", ["Social Studies"], CourseDuration.OneSemester, 0.5m,
                            "A social science elective covering behavior, cognition, development, research methods, and applications of psychological concepts.",
                            []),
                        Option("sociology", "Sociology", ["Social Studies"], CourseDuration.OneSemester, 0.5m,
                            "A social science elective covering culture, institutions, groups, social change, and sociological perspectives.",
                            [])
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
            [Map("Statutory", "Mathematics", CoverageLevel.Primary)]);
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
            [Map("Statutory", "Science", CoverageLevel.Primary)]);
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
            string.Join(Environment.NewLine,
            [
                $"Explain major concepts and vocabulary in {title}.",
                "Apply course skills in written, oral, practical, creative, or problem-based work.",
                "Use appropriate texts, resources, tools, and evidence to support conclusions.",
                "Produce portfolio-ready evidence of learning, revision, and reflection."
            ]),
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
            "English Language Arts 12" => Lines("CommonLit high school texts | https://www.commonlit.org/", "Project Gutenberg public-domain literature | https://www.gutenberg.org/", "Purdue OWL writing resources | https://owl.purdue.edu/owl/", "Parent-selected novels, essays, speeches, and poetry"),
            "Math 12" => Lines("Khan Academy high school math | https://www.khanacademy.org/math/high-school-math", "CK-12 math resources | https://www.ck12.org/", "OpenStax Algebra and Statistics chapters as needed | https://openstax.org/subjects/math", "Parent-created application problems"),
            "Pre-Algebra" => Lines("Khan Academy pre-algebra | https://www.khanacademy.org/math/pre-algebra", "CK-12 Pre-Algebra | https://www.ck12.org/", "OpenStax Prealgebra | https://openstax.org/details/books/prealgebra-2e", "Parent-created practice and real-life problems"),
            "Algebra I" => Lines("Khan Academy Algebra 1 | https://www.khanacademy.org/math/algebra", "CK-12 Algebra | https://www.ck12.org/", "OpenStax Elementary Algebra | https://openstax.org/details/books/elementary-algebra-2e", "Desmos graphing activities | https://www.desmos.com/"),
            "Geometry" => Lines("Khan Academy Geometry | https://www.khanacademy.org/math/geometry", "CK-12 Geometry | https://www.ck12.org/", "Illustrative Mathematics geometry tasks | https://tasks.illustrativemathematics.org/", "Desmos geometry and graphing activities | https://www.desmos.com/"),
            "Algebra II" => Lines("Khan Academy Algebra 2 | https://www.khanacademy.org/math/algebra2", "CK-12 Algebra II | https://www.ck12.org/", "OpenStax College Algebra | https://openstax.org/details/books/college-algebra-2e", "Desmos activities | https://www.desmos.com/"),
            "Trigonometry" => Lines("Khan Academy Trigonometry | https://www.khanacademy.org/math/trigonometry", "CK-12 Trigonometry | https://www.ck12.org/", "OpenStax Precalculus trigonometry chapters | https://openstax.org/books/precalculus/pages/index", "Desmos graphing activities | https://www.desmos.com/"),
            "Precalculus" => Lines("OpenStax Precalculus | https://openstax.org/details/books/precalculus-2e", "Khan Academy Precalculus | https://www.khanacademy.org/math/precalculus", "CK-12 Precalculus | https://www.ck12.org/", "Desmos graphing activities | https://www.desmos.com/"),
            "Calculus I" => Lines("OpenStax Calculus Volume 1 | https://openstax.org/details/books/calculus-volume-1", "Khan Academy Calculus | https://www.khanacademy.org/math/calculus-1", "MIT OpenCourseWare single-variable calculus | https://ocw.mit.edu/courses/18-01sc-single-variable-calculus-fall-2010/", "Desmos graphing tools | https://www.desmos.com/"),
            "Calculus II" => Lines("OpenStax Calculus Volume 2 | https://openstax.org/details/books/calculus-volume-2", "Khan Academy Calculus | https://www.khanacademy.org/math/calculus-2", "MIT OpenCourseWare single-variable calculus | https://ocw.mit.edu/courses/18-01sc-single-variable-calculus-fall-2010/", "Graphing and symbolic tools as appropriate"),
            "Calculus III" => Lines("OpenStax Calculus Volume 3 | https://openstax.org/details/books/calculus-volume-3", "MIT OpenCourseWare multivariable calculus | https://ocw.mit.edu/courses/18-02sc-multivariable-calculus-fall-2010/", "3D graphing or visualization tools"),
            "Physics" => Lines("OpenStax Physics | https://openstax.org/books/physics/pages/index", "OpenStax College Physics 2e | https://openstax.org/details/books/college-physics-2e", "PhET simulations | https://phet.colorado.edu/", "Home lab demonstrations or documented investigations"),
            "Environmental Science" => Lines("CK-12 Environmental Science | https://www.ck12.org/", "EPA student resources | https://www.epa.gov/students", "NOAA education resources | https://www.noaa.gov/education", "Local field observations and data collection"),
            "Anatomy and Physiology" => Lines("OpenStax Anatomy and Physiology 2e | https://openstax.org/details/books/anatomy-and-physiology-2e", "Khan Academy health and medicine | https://www.khanacademy.org/science/health-and-medicine", "Parent-approved labs or models"),
            "Chemistry" => Lines("OpenStax Chemistry 2e | https://openstax.org/details/books/chemistry-2e", "PhET chemistry simulations | https://phet.colorado.edu/", "Khan Academy Chemistry | https://www.khanacademy.org/science/chemistry", "Safe home lab demonstrations"),
            "Advanced Biology" => Lines("OpenStax Biology 2e | https://openstax.org/details/books/biology-2e", "HHMI BioInteractive | https://www.biointeractive.org/", "Khan Academy Biology | https://www.khanacademy.org/science/biology", "Microscope, field, or model-based investigations"),
            "Earth and Space Science" => Lines("CK-12 Earth Science | https://www.ck12.org/", "NASA learning resources | https://www.nasa.gov/learning-resources/", "NOAA education resources | https://www.noaa.gov/education", "Sky observation and local geology records"),
            "Forensic Science" => Lines("National Institute of Justice forensic science topics | https://nij.ojp.gov/topics/forensics", "Open educational forensic science readings", "Case-study packets", "Safe observation, measurement, chemistry, and biology demonstrations"),
            "Astronomy" => Lines("OpenStax Astronomy 2e | https://openstax.org/details/books/astronomy-2e", "NASA learning resources | https://www.nasa.gov/learning-resources/", "Sky observation logs", "Planetarium or observatory resources where available"),
            "Government and Economics" => Lines("iCivics | https://www.icivics.org/", "National Constitution Center | https://constitutioncenter.org/", "OpenStax American Government 3e | https://openstax.org/details/books/american-government-3e/", "CFPB youth financial education | https://www.consumerfinance.gov/consumer-tools/educator-tools/youth-financial-education/"),
            "Government and Civics" => Lines("iCivics | https://www.icivics.org/", "National Constitution Center | https://constitutioncenter.org/", "OpenStax American Government 3e | https://openstax.org/details/books/american-government-3e/", "Local and state government sources"),
            "Economics" => Lines("OpenStax Principles of Economics 3e | https://openstax.org/details/books/principles-economics-3e", "Khan Academy economics | https://www.khanacademy.org/economics-finance-domain", "CFPB consumer tools | https://www.consumerfinance.gov/consumer-tools/"),
            "U.S. History" => Lines("OpenStax U.S. History | https://openstax.org/details/books/us-history", "Library of Congress primary sources | https://www.loc.gov/", "National Archives DocsTeach | https://www.docsteach.org/"),
            "World History" => Lines("OpenStax World History Volume 2 | https://openstax.org/details/books/world-history-volume-2", "World History for Us All | https://whfua.history.ucla.edu/", "OER Project | https://www.oerproject.com/", "Primary source excerpts"),
            "Psychology" => Lines("OpenStax Psychology 2e | https://openstax.org/details/books/psychology-2e", "APA high school psychology resources | https://www.apa.org/education-career/k12", "Teacher-selected case studies and reflection prompts"),
            "Sociology" => Lines("OpenStax Introduction to Sociology 3e | https://openstax.org/details/books/introduction-sociology-3e", "U.S. Census data | https://www.census.gov/", "Teacher-selected articles and observation activities"),
            "Personal Finance" => Lines("CFPB youth financial education | https://www.consumerfinance.gov/consumer-tools/educator-tools/youth-financial-education/", "FDIC Money Smart | https://www.fdic.gov/resources/consumers/money-smart", "Next Gen Personal Finance | https://www.ngpf.org/", "Practical household budgeting exercises"),
            "Physical Education and Health" => Lines("CDC school health resources | https://www.cdc.gov/healthyschools/", "MedlinePlus | https://medlineplus.gov/", "SHAPE America | https://www.shapeamerica.org/", "Fitness logs and parent-approved activity plans"),
            "Experiential Capstone" => Lines("Purdue OWL research and citation resources | https://owl.purdue.edu/owl/research_and_citation/", "Parent-selected project resources", "Interviews or mentorship notes", "Research sources", "Project log", "Portfolio artifacts", "Final presentation materials"),
            _ when title.Contains("Computer Science", StringComparison.OrdinalIgnoreCase) => Lines("Code.org | https://code.org/", "freeCodeCamp | https://www.freecodecamp.org/", "Khan Academy computing | https://www.khanacademy.org/computing", "Project repository or notebook"),
            _ when title.Contains("Career", StringComparison.OrdinalIgnoreCase) => Lines("Bureau of Labor Statistics Occupational Outlook Handbook | https://www.bls.gov/ooh/", "CareerOneStop | https://www.careeronestop.org/", "Interview notes", "Resume and planning templates"),
            _ when title.Contains("Creative Writing", StringComparison.OrdinalIgnoreCase) => Lines("Writing prompts", "Mentor texts", "Purdue OWL | https://owl.purdue.edu/owl/", "NaNoWriMo Young Writers Program | https://ywp.nanowrimo.org/", "Revision workshop notes"),
            _ when title.Contains("Entrepreneurship", StringComparison.OrdinalIgnoreCase) => Lines("U.S. Small Business Administration | https://www.sba.gov/", "SCORE resources | https://www.score.org/", "Business model canvas", "Budgeting worksheets", "Customer discovery notes"),
            _ when title.Contains("Independent Research", StringComparison.OrdinalIgnoreCase) => Lines("Purdue OWL research and citation resources | https://owl.purdue.edu/owl/research_and_citation/", "Library databases or public sources", "Citation guide", "Research notebook", "Outline drafts", "Final paper or presentation"),
            _ when title.Contains("College", StringComparison.OrdinalIgnoreCase) => Lines("College Board BigFuture | https://bigfuture.collegeboard.org/", "Federal Student Aid | https://studentaid.gov/", "Application checklists", "Study planning templates"),
            _ when title.Contains("Work-Based", StringComparison.OrdinalIgnoreCase) => Lines("CareerOneStop skills and career resources | https://www.careeronestop.org/", "Supervisor or mentor feedback", "Work logs", "Employability skill rubrics", "Safety and workplace policy materials"),
            _ when title.Contains("Spanish", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("French", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("German", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Chinese", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Japanese", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Arabic", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Italian", StringComparison.OrdinalIgnoreCase) ||
                title == "American Sign Language" ||
                title == "Latin" => Lines("ACTFL Can-Do Statements | https://www.actfl.org/educator-resources/ncssfl-actfl-can-do-statements", "Parent-selected language text or online course", "Conversation practice", "Vocabulary notebook", "Cultural readings and media"),
            _ when title.Contains("Art", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Drawing", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Painting", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Photography", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Design", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Ceramics", StringComparison.OrdinalIgnoreCase) => Lines("Khan Academy art history | https://www.khanacademy.org/humanities/art-history", "Museum education resources", "Artist examples", "Sketchbook or process journal", "Portfolio artifacts"),
            _ when title.Contains("Theater", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Choir", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Music", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Band", StringComparison.OrdinalIgnoreCase) => Lines("Khan Academy music resources | https://www.khanacademy.org/humanities/music", "Parent-selected repertoire or script", "Performance recordings", "Music theory or theater resources", "Rehearsal log", "Critique notes"),
            _ => Lines("Khan Academy | https://www.khanacademy.org/", "Parent-selected spine text or course platform", "Open educational resources", "Notebooks", "Project evidence", "Portfolio artifacts")
        };
    }

    private static string Lines(params string[] values)
    {
        return string.Join(Environment.NewLine, values);
    }
}

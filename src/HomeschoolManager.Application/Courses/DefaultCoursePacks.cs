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
                            [Map("Statutory", "Civics", CoverageLevel.Primary), Map("MDE Summary", "U.S. Constitution", CoverageLevel.Primary), Map("MDE Summary", "Michigan Constitution", CoverageLevel.Primary)]),
                        Option("government-civics", "Government and Civics", ["Social Studies", "Civics"], CourseDuration.OneSemester, 0.5m,
                            "A one-semester government and civics course covering constitutional principles, citizenship, rights, responsibilities, and civic participation.",
                            [Map("Statutory", "Civics", CoverageLevel.Primary), Map("MDE Summary", "U.S. Constitution", CoverageLevel.Primary), Map("MDE Summary", "Michigan Constitution", CoverageLevel.Primary)]),
                        Option("economics", "Economics", ["Social Studies", "Economics"], CourseDuration.OneSemester, 0.5m,
                            "A one-semester economics course covering personal, microeconomic, macroeconomic, or applied economic concepts.",
                            []),
                        Option("psychology", "Psychology", ["Social Studies"], CourseDuration.OneSemester, 0.5m,
                            "A social science elective covering behavior, cognition, development, research methods, and applications of psychological concepts.",
                            []),
                        Option("sociology", "Sociology", ["Social Studies"], CourseDuration.OneSemester, 0.5m,
                            "A social science elective covering culture, institutions, groups, social change, and sociological perspectives.",
                            [])
                    ]),
                Choice("history", "History", "us-history-geography",
                    [
                        UnitedStatesHistoryOption("us-history-geography", "U.S. History and Geography", "A history course covering major eras in United States history, geographic context, civic development, primary-source interpretation, and continuity and change over time."),
                        HistoryOption("world-history-geography", "World History and Geography", "A history course covering global eras, geography, culture, conflict, exchange, migration, and historical inquiry."),
                        HistoryOption("modern-world-history", "Modern World History", "A history course emphasizing global change from the age of revolutions through the contemporary era, including nationalism, imperialism, war, decolonization, globalization, and human rights."),
                        UnitedStatesHistoryOption("ap-us-history", "Advanced U.S. History", "An advanced history course emphasizing college-preparatory reading, source analysis, argument writing, and major themes in United States history."),
                        HistoryOption("ap-world-history", "Advanced World History", "An advanced history course emphasizing global historical processes, comparison, causation, continuity and change, and evidence-based historical argument."),
                        HistoryOption("european-history", "European History", "A history course covering major European political, cultural, intellectual, economic, and social developments in regional and global context.")
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

    private static CourseTemplateOptionDefinition HistoryOption(string id, string title, string description)
    {
        return Option(
            id,
            title,
            ["History", "Social Studies"],
            CourseDuration.TwoSemesters,
            1,
            description,
            [Map("Statutory", "History", CoverageLevel.Primary)]);
    }

    private static CourseTemplateOptionDefinition UnitedStatesHistoryOption(string id, string title, string description)
    {
        return Option(
            id,
            title,
            ["History", "Social Studies"],
            CourseDuration.TwoSemesters,
            1,
            description,
            [Map("Statutory", "History", CoverageLevel.Primary), Map("MDE Summary", "U.S. Constitution", CoverageLevel.Secondary), Map("MDE Summary", "Michigan Constitution", CoverageLevel.Secondary)]);
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
            LearningObjectivesFor(title),
            "",
            duration == CourseDuration.TwoSemesters
                ? $"Semester 1: foundations, core vocabulary, guided practice, and early projects. Semester 2: advanced topics, independent application, review, and a final portfolio or capstone evidence set for {subjectText}."
                : $"Weeks 1-4: foundations and vocabulary. Weeks 5-10: guided practice and applied work. Weeks 11-16: independent application, review, and final evidence set for {subjectText}.",
            "Imported pack plan. Parent should customize resources, pacing, assignments, assessment evidence, and grading notes to match the actual course.");
    }

    private static string LearningObjectivesFor(string title)
    {
        return title switch
        {
            "English Language Arts 12" => Lines(
                "Analyze literary works for theme, structure, style, historical context, and use of textual evidence.",
                "Write clear analytical, argumentative, narrative, and research-based pieces with revision and source documentation.",
                "Apply grammar, usage, vocabulary, spelling, and editing conventions in polished written work.",
                "Discuss and present interpretations of texts using evidence and respectful academic conversation."),
            "Government and Economics" => Lines(
                "Explain constitutional principles, rights, responsibilities, branches of government, and federalism.",
                "Evaluate civic issues, public policy questions, and citizenship responsibilities using evidence.",
                "Apply economic reasoning to scarcity, incentives, markets, government policy, and personal financial decisions.",
                "Connect civic participation and economic decision-making to current events and household or community choices."),
            "Government and Civics" => Lines(
                "Explain the philosophical and constitutional foundations of American government.",
                "Describe the structure and powers of local, state, and federal government.",
                "Evaluate rights, liberties, responsibilities, elections, public policy, and civic participation.",
                "Use civic evidence to support a reasoned position on a public issue."),
            "Economics" => Lines(
                "Apply scarcity, incentives, opportunity cost, supply, demand, and market concepts to real decisions.",
                "Explain how households, firms, governments, and financial institutions interact in the economy.",
                "Interpret economic indicators and connect them to national and international economic conditions.",
                "Use economic reasoning to evaluate personal, public, and business choices."),
            "U.S. History and Geography" or "U.S. History" => Lines(
                "Analyze major developments in United States history using chronology, geography, and historical evidence.",
                "Interpret primary and secondary sources for perspective, reliability, context, and argument.",
                "Explain how constitutional change, reform movements, conflict, migration, industry, and civil rights shaped the United States.",
                "Write evidence-based historical explanations about continuity, change, cause, and consequence."),
            "World History and Geography" or "World History" => Lines(
                "Analyze major global eras using geography, chronology, and historical inquiry.",
                "Compare societies, belief systems, trade networks, conflicts, revolutions, and migrations across regions.",
                "Interpret primary and secondary sources for perspective, context, and evidence.",
                "Write evidence-based historical explanations about global continuity, change, and interconnection."),
            "Modern World History" => Lines(
                "Explain major modern global developments including revolutions, industrialization, imperialism, nationalism, world wars, decolonization, and globalization.",
                "Analyze maps, data, and historical sources to explain modern global change.",
                "Compare political, economic, and social transformations across world regions.",
                "Construct evidence-based arguments about human rights, conflict, migration, technology, and global interdependence."),
            "Advanced U.S. History" => Lines(
                "Analyze United States history through college-preparatory themes such as identity, politics, work, migration, culture, and America in the world.",
                "Evaluate complex primary and secondary sources for argument, sourcing, corroboration, and historical context.",
                "Write thesis-driven historical arguments using specific evidence and historical reasoning.",
                "Connect major eras of United States history to enduring civic, social, economic, and geographic questions."),
            "Advanced World History" => Lines(
                "Analyze global historical processes through comparison, causation, continuity, and change over time.",
                "Evaluate primary and secondary sources across cultures and regions for evidence and perspective.",
                "Write thesis-driven historical arguments about global patterns, interactions, and transformations.",
                "Explain how trade, belief systems, states, technology, migration, conflict, and environment shaped world history."),
            "European History" => Lines(
                "Explain major European political, cultural, intellectual, religious, economic, and social developments.",
                "Analyze European history in regional, global, and chronological context.",
                "Interpret historical sources for perspective, context, argument, and evidence.",
                "Write evidence-based explanations about continuity, change, causation, and comparison in European history."),
            "Personal Finance" => Lines(
                "Build and revise a budget using income, expenses, saving goals, taxes, and tradeoffs.",
                "Explain banking, credit, debt, insurance, investing, and consumer-protection concepts.",
                "Compare financial products and decisions using risk, cost, benefit, and long-term impact.",
                "Apply mathematical reasoning to practical financial scenarios."),
            "Math 12" => Lines(
                "Use algebra, functions, statistics, and proportional reasoning to solve practical senior-level problems.",
                "Model real-world situations with equations, graphs, tables, and written explanations.",
                "Interpret quantitative information in financial, civic, scientific, and household contexts.",
                "Communicate solution strategies and check answers for reasonableness."),
            "Pre-Algebra" => Lines(
                "Use operations with whole numbers, fractions, decimals, integers, ratios, and proportions accurately.",
                "Translate word problems into expressions, equations, diagrams, or tables.",
                "Solve one-step and multi-step equations and inequalities with clear reasoning.",
                "Apply number sense and proportional reasoning to measurement, geometry, and everyday problems."),
            "Algebra I" => Lines(
                "Solve and graph linear equations, inequalities, and systems in mathematical and applied contexts.",
                "Represent functions with tables, graphs, equations, and verbal descriptions.",
                "Use exponents, polynomials, factoring foundations, and data analysis to solve problems.",
                "Explain algebraic reasoning and interpret solutions in context."),
            "Geometry" => Lines(
                "Use definitions, postulates, and theorems to reason about congruence, similarity, circles, and transformations.",
                "Construct logical arguments and proofs using diagrams, coordinates, and written explanations.",
                "Solve measurement problems involving area, volume, right triangles, and geometric modeling.",
                "Apply coordinate geometry and transformations to analyze figures and real-world designs."),
            "Algebra II" => Lines(
                "Analyze linear, quadratic, polynomial, rational, radical, exponential, and logarithmic functions.",
                "Solve systems, equations, inequalities, and modeling problems with appropriate algebraic methods.",
                "Use sequences, series, complex numbers, and function transformations in advanced problem solving.",
                "Explain how algebraic models represent patterns, data, and real-world relationships."),
            "Trigonometry" => Lines(
                "Use right-triangle and unit-circle trigonometry to solve mathematical and applied problems.",
                "Graph and analyze trigonometric functions, inverse functions, and transformations.",
                "Apply identities, vectors, and trigonometric equations with accurate notation and reasoning.",
                "Model periodic and geometric situations using trigonometric relationships."),
            "Precalculus" => Lines(
                "Analyze advanced functions including polynomial, rational, exponential, logarithmic, and trigonometric models.",
                "Use trigonometry, analytic geometry, sequences, and limits concepts to prepare for calculus.",
                "Model and interpret quantitative relationships with graphs, equations, technology, and written reasoning.",
                "Solve multi-step problems and explain how function behavior supports conclusions."),
            "Calculus I" => Lines(
                "Evaluate limits and explain continuity, rates of change, and accumulation.",
                "Compute derivatives and use them to analyze motion, optimization, graph behavior, and related rates.",
                "Compute definite and indefinite integrals and connect them to area and accumulation.",
                "Use the fundamental theorem of calculus to connect differentiation and integration."),
            "Calculus II" => Lines(
                "Apply integration techniques to solve area, volume, work, and other accumulation problems.",
                "Analyze sequences and series for convergence, approximation, and representation of functions.",
                "Use parametric, polar, or differential-equation concepts where appropriate to extend calculus reasoning.",
                "Explain multi-step calculus solutions with accurate notation and interpretation."),
            "Calculus III" => Lines(
                "Use vectors, three-dimensional coordinates, and multivariable functions to model space and change.",
                "Compute and interpret partial derivatives, gradients, and optimization in several variables.",
                "Evaluate multiple integrals and connect them to area, volume, mass, or accumulation.",
                "Explain multivariable calculus concepts with diagrams, notation, and contextual interpretation."),
            "Physics" => Lines(
                "Model motion, forces, energy, momentum, waves, electricity, and magnetism using diagrams, equations, and evidence.",
                "Use labs, simulations, or demonstrations to collect and interpret physical data.",
                "Apply conservation laws and scientific reasoning to explain physical systems.",
                "Communicate physics conclusions using units, graphs, calculations, and written explanations."),
            "Environmental Science" => Lines(
                "Explain ecosystem interactions, biodiversity, resource use, pollution, conservation, and human environmental impact.",
                "Use field observations, data, maps, or case studies to evaluate environmental questions.",
                "Analyze tradeoffs in environmental decisions using scientific evidence.",
                "Connect local observations to broader ecological, climate, and resource patterns."),
            "Anatomy and Physiology" => Lines(
                "Explain the structure and function of major human body systems.",
                "Connect cells, tissues, organs, and systems to health, homeostasis, and disease prevention.",
                "Use diagrams, models, observations, or labs to support anatomical and physiological explanations.",
                "Apply body-system knowledge to practical health and wellness scenarios."),
            "Chemistry" => Lines(
                "Explain matter, atomic structure, periodic trends, bonding, reactions, stoichiometry, and solutions.",
                "Use chemical equations, models, calculations, and lab or simulation evidence to support claims.",
                "Apply conservation of matter and particle-level reasoning to chemical changes.",
                "Communicate chemistry results with accurate units, formulas, vocabulary, and safety awareness."),
            "Advanced Biology" => Lines(
                "Analyze cells, genetics, evolution, ecology, anatomy, physiology, or biotechnology using biological evidence.",
                "Use models, microscopy, fieldwork, simulations, or data to explain living systems.",
                "Evaluate biological claims using source quality, data patterns, and scientific reasoning.",
                "Connect biological concepts to health, environment, technology, or ethical questions."),
            "Earth and Space Science" => Lines(
                "Explain earth systems, geology, weather, climate, astronomy, and natural hazards using scientific models.",
                "Interpret maps, data, observations, and diagrams related to Earth and space processes.",
                "Analyze interactions among atmosphere, hydrosphere, geosphere, biosphere, and human activity.",
                "Use evidence to explain natural processes and their effects on local and global systems."),
            "Forensic Science" => Lines(
                "Apply observation, measurement, biology, chemistry, and physics concepts to evidence analysis.",
                "Document investigations with accurate notes, data, reasoning, and chain-of-custody awareness.",
                "Evaluate case evidence for reliability, limits, and scientific support.",
                "Communicate forensic conclusions clearly while distinguishing evidence from inference."),
            "Astronomy" => Lines(
                "Explain solar system, stellar, galactic, and cosmological concepts using models and evidence.",
                "Use observation logs, simulations, spectra, diagrams, or data to study astronomical objects.",
                "Analyze how gravity, light, scale, and motion shape astronomical systems.",
                "Communicate astronomy explanations with attention to evidence, uncertainty, and scientific models."),
            "Physical Education and Health" => Lines(
                "Create and document a personal fitness and wellness plan.",
                "Explain nutrition, mental health, safety, disease prevention, and substance-awareness concepts.",
                "Track physical activity and reflect on habits that support lifelong wellness.",
                "Apply health information to responsible personal decision-making."),
            "Experiential Capstone" => Lines(
                "Define a focused question, problem, or project goal and revise it through feedback.",
                "Integrate research, applied work, documentation, and reflection into a coherent project record.",
                "Produce a final product, presentation, portfolio, or demonstration for parent review.",
                "Explain how the capstone connects academic learning to practical experience."),
            "Career Exploration" => Lines(
                "Research career pathways using labor-market, education, skill, and workplace information.",
                "Compare career options using interests, aptitudes, income, training, lifestyle, and values.",
                "Create planning artifacts such as a resume, interview notes, pathway comparison, or transition plan.",
                "Reflect on practical experiences, conversations, or observations related to career readiness."),
            "Computer Science" => Lines(
                "Write, test, debug, and explain programs using core programming concepts.",
                "Use algorithms, decomposition, variables, control flow, data, and functions to solve problems.",
                "Document computing projects with clear design choices, revisions, and evidence of testing.",
                "Explain ethical, practical, or technical implications of digital systems."),
            "Creative Writing" => Lines(
                "Draft, revise, and polish original creative work in selected genres.",
                "Use literary techniques such as imagery, voice, structure, dialogue, character, setting, and pacing.",
                "Participate in critique or reflection to improve clarity, craft, and audience impact.",
                "Build a writing portfolio that documents revision and finished pieces."),
            "Entrepreneurship" => Lines(
                "Develop a business or venture concept using customer needs, value proposition, costs, and revenue.",
                "Create planning artifacts such as a budget, market notes, pitch, prototype, or operations plan.",
                "Evaluate entrepreneurial decisions using risk, ethics, feasibility, and evidence from feedback.",
                "Reflect on practical venture work, revisions, and lessons learned."),
            "Independent Research" => Lines(
                "Develop a focused research question and revise it through source review and feedback.",
                "Locate, evaluate, cite, and synthesize credible sources.",
                "Produce a research paper, presentation, or project using clear organization and evidence.",
                "Document research process, revisions, findings, and limitations."),
            "College and Career Readiness" => Lines(
                "Create a postsecondary transition plan that addresses goals, applications, finances, and timelines.",
                "Compare college, training, work, or service pathways using credible planning resources.",
                "Practice communication, study systems, organization, and self-advocacy skills.",
                "Prepare records, checklists, reflections, or applications that support the transition plan."),
            "Work-Based Learning" => Lines(
                "Document workplace responsibilities, safety expectations, employability skills, and supervisor or mentor feedback.",
                "Connect work tasks to academic, technical, communication, or problem-solving skills.",
                "Reflect on professional habits, growth, challenges, and career implications.",
                "Compile logs, artifacts, evaluations, or demonstrations as evidence of applied learning."),
            _ when title.Contains("Spanish", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("French", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("German", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Chinese", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Japanese", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Arabic", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Italian", StringComparison.OrdinalIgnoreCase) ||
                title == "American Sign Language" ||
                title == "Latin" => Lines(
                    $"Use {title} vocabulary and grammar in interpretive, interpersonal, and presentational communication.",
                    $"Read, view, listen to, or sign {title} materials for meaning, context, and cultural understanding.",
                    $"Produce spoken, signed, or written {title} communication appropriate to the student's level.",
                    $"Compare cultures connected with {title} study with respectful attention to context."),
            _ when title.Contains("Art", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Drawing", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Painting", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Photography", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Design", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Ceramics", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Theater", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Choir", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Music", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Band", StringComparison.OrdinalIgnoreCase) => Lines(
                    $"Apply {title} techniques, vocabulary, tools, and creative or performance processes.",
                    $"Develop, revise, and present {title} work as portfolio or performance evidence.",
                    $"Analyze artistic choices, examples, critique, and reflection using {title} vocabulary.",
                    $"Document growth, practice, revision, and finished work in {title}."),
            _ => Lines(
                $"Explain important concepts, vocabulary, and methods in {title}.",
                "Complete documented assignments or projects that show growth and understanding.",
                "Use appropriate sources, tools, and evidence for the course.",
                "Prepare portfolio-ready evidence for parent review.")
        };
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
            "U.S. History and Geography" => "Historical inquiry; geography; constitutional development; industrialization; reform; conflict; civil rights; modern America; primary-source evidence.",
            "U.S. History" => "Founding; constitutional development; reform; conflict; industrialization; civil rights; modern America; historical evidence.",
            "World History and Geography" => "Historical inquiry; geography; global exchange; belief systems; revolutions; imperialism; conflict; globalization; primary-source evidence.",
            "World History" => "Ancient and classical societies; global exchange; belief systems; revolutions; conflict; globalization; historical inquiry.",
            "Modern World History" => "Revolutions; industrialization; nationalism; imperialism; world wars; decolonization; globalization; human rights; migration; international systems.",
            "Advanced U.S. History" => "Historical argument; source analysis; national identity; politics and power; work and exchange; migration; civil rights; America in the world.",
            "Advanced World History" => "Global processes; comparison; causation; continuity and change; trade networks; states; belief systems; migration; conflict; environment.",
            "European History" => "Renaissance; Reformation; absolutism; Enlightenment; revolution; industrialization; nationalism; war; integration; European global influence.",
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
            "U.S. History and Geography" => Lines("OpenStax U.S. History | https://openstax.org/details/books/us-history", "Library of Congress primary sources | https://www.loc.gov/", "National Archives DocsTeach | https://www.docsteach.org/", "Historical maps and geography resources"),
            "U.S. History" => Lines("OpenStax U.S. History | https://openstax.org/details/books/us-history", "Library of Congress primary sources | https://www.loc.gov/", "National Archives DocsTeach | https://www.docsteach.org/"),
            "World History and Geography" => Lines("OpenStax World History Volume 2 | https://openstax.org/details/books/world-history-volume-2", "World History for Us All | https://whfua.history.ucla.edu/", "OER Project | https://www.oerproject.com/", "Historical maps and primary source excerpts"),
            "World History" => Lines("OpenStax World History Volume 2 | https://openstax.org/details/books/world-history-volume-2", "World History for Us All | https://whfua.history.ucla.edu/", "OER Project | https://www.oerproject.com/", "Primary source excerpts"),
            "Modern World History" => Lines("OpenStax World History Volume 2 | https://openstax.org/details/books/world-history-volume-2", "OER Project | https://www.oerproject.com/", "United Nations human rights resources | https://www.un.org/en/about-us/universal-declaration-of-human-rights", "Primary source excerpts and historical maps"),
            "Advanced U.S. History" => Lines("OpenStax U.S. History | https://openstax.org/details/books/us-history", "Library of Congress primary sources | https://www.loc.gov/", "National Archives DocsTeach | https://www.docsteach.org/", "College Board AP U.S. History course overview | https://apcentral.collegeboard.org/courses/ap-united-states-history"),
            "Advanced World History" => Lines("OpenStax World History Volume 2 | https://openstax.org/details/books/world-history-volume-2", "OER Project | https://www.oerproject.com/", "World History for Us All | https://whfua.history.ucla.edu/", "College Board AP World History course overview | https://apcentral.collegeboard.org/courses/ap-world-history"),
            "European History" => Lines("OpenStax World History Volume 2 | https://openstax.org/details/books/world-history-volume-2", "EuroDocs primary sources | https://eudocs.lib.byu.edu/index.php/Main_Page", "College Board AP European History course overview | https://apcentral.collegeboard.org/courses/ap-european-history", "Museum and archive primary sources"),
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
